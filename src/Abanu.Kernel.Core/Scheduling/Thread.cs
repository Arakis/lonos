﻿// This file is part of Abanu, an Operating System written in C#. Web: https://www.abanu.org
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using Abanu.Kernel.Core.Interrupts;
using Abanu.Kernel.Core.MemoryManagement;
using Abanu.Kernel.Core.PageManagement;
using Abanu.Kernel.Core.Processes;
using Mosa.Runtime;
using Mosa.Runtime.x86;

namespace Abanu.Kernel.Core.Scheduling
{
    public unsafe class Thread
    {
        public ThreadStatus Status = ThreadStatus.Empty;
        public Addr StackBottom;
        public Addr StackTop;
        //public IntPtr StackStatePointer;
        public IDTTaskStack* StackState;
        public Addr KernelStack = null;
        public Addr KernelStackBottom = null;
        public USize KernelStackSize = null;
        public uint Ticks;
        public bool User;
        public Process Process;
        public uint DataSelector;
        public uint ThreadID;
        public bool Debug;
        public string DebugName;
        public uint ArgumentBufferSize;
        public SystemMessage DebugSystemMessage;

        public bool CanScheduled
        {
            get
            {
                return Status == ThreadStatus.ScheduleForStart || Status == ThreadStatus.Running;
            }
        }

        /// <summary>
        /// 0: Default
        /// >0: Get Priority more interrupts
        /// <0: Skipping Priority interrupts
        /// </summary>
        public int Priority;
        internal int PriorityInterrupts;

        public Thread ChildThread;
        public Thread ParentThread;

        public void SetArgument(uint offsetBytes, uint value)
        {
            var argAddr = (uint*)GetArgumentAddr(offsetBytes);
            argAddr[0] = value;
        }

        public Addr GetArgumentAddr(uint offsetBytes)
        {
            return StackBottom - ArgumentBufferSize + offsetBytes - 4;
        }

        public void FreeMemory()
        {
            VirtualPageManager.FreeAddr(StackTop);
            if (User)
                VirtualPageManager.FreeAddr(StackState);
            VirtualPageManager.FreeAddr(KernelStack);
        }

        public void Start()
        {
            Status = ThreadStatus.ScheduleForStart;
        }

        public void Terminate()
        {
            Status = ThreadStatus.Terminated;
            if (ChildThread != null)
            {
                ChildThread.Terminate();
                ChildThread = null;
            }
            ParentThread = null;
        }

    }

    public class KernelThread : Thread
    {
    }

    public class UserThread : Thread
    {
    }

}
