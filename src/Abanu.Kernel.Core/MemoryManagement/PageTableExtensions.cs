﻿// This file is part of Abanu, an Operating System written in C#. Web: https://www.abanu.org
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abanu.Kernel.Core.Boot;
using Abanu.Kernel.Core.PageManagement;

namespace Abanu.Kernel.Core.MemoryManagement
{
    /// <summary>
    /// Extension methods for the <see cref="IPageTable"/> interface
    /// </summary>
    public static unsafe class PageTableExtensions
    {
        /// <summary>
        /// Sync specific mappings with another table.
        /// The virtual and physical Addresses in both table will be the same.
        /// </summary>
        public static void MapCopy(this IPageTable table, IPageTable fromTable, BootInfoMemoryType type, bool present = true, bool flush = false)
        {
            var mm = BootInfo.GetMap(type);
            table.MapCopy(fromTable, mm->Start, mm->Size, present, flush);
        }

        /// <summary>
        /// Sync specific mappings with another table.
        /// The virtual and physical Addresses in both table will be the same.
        /// </summary>
        public static void MapCopy(this IPageTable table, IPageTable fromTable, KernelMemoryMap* mm, bool present = true, bool flush = false)
        {
            table.MapCopy(fromTable, mm->Start, mm->Size, present, flush);
        }

        public static unsafe void SetWritable(this IPageTable table, BootInfoMemoryType type)
        {
            var mm = BootInfo.GetMap(type);
            SetWritable(table, mm->Start, mm->Size);
        }

        public static unsafe void SetWritable(this IPageTable table, uint virtAddr, uint size)
        {
            if (!KConfig.UseKernelMemoryProtection)
                return;

            table.SetWritable(virtAddr, size);
        }

        public static unsafe void SetExecutable(this IPageTable table, BootInfoMemoryType type)
        {
            var mm = BootInfo.GetMap(type);
            SetExecutable(table, mm->Start, mm->Size);
        }

        public static unsafe void SetExecutable(this IPageTable table, uint virtAddr, uint size)
        {
            if (!KConfig.UseKernelMemoryProtection)
                return;

            table.SetExecutable(virtAddr, size);
        }

    }
}
