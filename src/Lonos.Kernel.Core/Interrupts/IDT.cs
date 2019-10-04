﻿// This file is part of Lonos Project, an Operating System written in C#. Web: https://www.lonos.io
// Licensed under the GNU 2.0 license. See LICENSE.txt file in the project root for full license information.

using System;
using Lonos.Kernel.Core;
using Lonos.Kernel.Core.Diagnostics;
using Lonos.Kernel.Core.Interrupts;
using Lonos.Kernel.Core.PageManagement;
using Lonos.Kernel.Core.Scheduling;
using Mosa.Runtime;
using Mosa.Runtime.x86;

//TODO: Name in compiler
#pragma warning disable SA1300 // Element should begin with upper-case letter
namespace Lonos.Kernel.Core
#pragma warning restore SA1300 // Element should begin with upper-case letter
{

    /// <summary>
    /// IDT
    /// </summary>
    public static unsafe class IDT
    {

        /// <summary>
        /// Interrupts the handler.
        /// </summary>
        /// <param name="stackStatePointer">The stack state pointer.</param>
        private static unsafe void ProcessInterrupt(uint stackStatePointer)
        {
            ushort dataSelector = 0x10;
            Native.SetSegments(dataSelector, dataSelector, dataSelector, dataSelector, dataSelector);
            var block = (InterruptControlBlock*)Address.InterruptControlBlock;
            Native.SetCR3(block->KernelPageTableAddr);

            //KernelMessage.WriteLine("Interrupt occurred");

            var stack = (IDTStack*)stackStatePointer;
            var irq = stack->Interrupt;

            uint pageTableAddr = 0;
            var thread = Scheduler.GetCurrentThread();
            if (thread != null)
            {
                dataSelector = (ushort)thread.DataSelector;
                pageTableAddr = thread.Process.PageTable.GetPageTablePhysAddr();
            }

            if (!IDTManager.Enabled)
            {
                PIC.SendEndOfInterrupt(irq);
                return;
            }

            var interruptInfo = IDTManager.Handlers[irq];
            if (KConfig.Trace.Interrupts && interruptInfo.Trace && thread != null)
                KernelMessage.WriteLine("Interrupt {0}, Thread {1}, EIP={2:X8} ESP={3:X8}", irq, thread.ThreadID, stack->EIP, stack->ESP);

            IDTManager.RaisedCount++;

            if (interruptInfo.CountStatistcs)
                IDTManager.RaisedCountCustom++;

            if (KConfig.Trace.Interrupts)
            {
                if (interruptInfo.Trace)
                    KernelMessage.WriteLine("Interrupt: {0}", irq);

                var col = Screen.Column;
                var row = Screen.Row;
                Screen.Column = 0;
                Screen.Goto(2, 35);
                Screen.Write("Interrupts: ");
                Screen.Write(IDTManager.RaisedCount);
                Screen.Goto(3, 35);
                Screen.Write("IntNoClock: ");
                Screen.Write(IDTManager.RaisedCountCustom);
                Screen.Row = row;
                Screen.Column = col;
            }

            if (irq < 0 || irq > 255)
                Panic.Error("Invalid Interrupt");

            if (interruptInfo.PreHandler != null)
                interruptInfo.PreHandler(stack);

            if (interruptInfo.Handler == null)
            {
                Panic.Error("Handler is null");
            }
            else
            {

            }

            interruptInfo.Handler(stack);

            PIC.SendEndOfInterrupt(irq);

            if (pageTableAddr > 0)
                Native.SetCR3(pageTableAddr);

            Native.SetSegments(dataSelector, dataSelector, dataSelector, dataSelector, 0x10);
        }

    }

}
