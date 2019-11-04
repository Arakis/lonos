﻿// This file is part of Abanu, an Operating System written in C#. Web: https://www.abanu.org
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using Abanu.Kernel.Core.Boot;
using Abanu.Kernel.Core.Devices;
using Abanu.Kernel.Core.Diagnostics;
using Abanu.Kernel.Core.PageManagement;
using Mosa.Runtime;

//using Mosa.Kernel.x86;

namespace Abanu.Kernel.Core.MemoryManagement.PageAllocators
{

    public unsafe class VirtualInitialPageAllocator : InitialPageAllocator2
    {

        private bool AllocRawSelfMemoryPhysical;

        public VirtualInitialPageAllocator(bool allocRawSelfMemoryPhysical)
        {
            AllocRawSelfMemoryPhysical = allocRawSelfMemoryPhysical;
        }

        protected override MemoryRegion AllocRawMemory(uint size)
        {
            if (AllocRawSelfMemoryPhysical)
                return AllocRawMemoryPhys(size);
            else
                return AllocRawMemoryVirt(size);
        }

        protected static MemoryRegion AllocRawMemoryPhys(uint size)
        {
            var kmap = PhysicalPageManager.AllocateRegion(size);
            KernelMemoryMapManager.Header->Used.Add(new KernelMemoryMap(kmap.Start, kmap.Size, BootInfoMemoryType.PageFrameAllocator, AddressSpaceKind.Virtual));
            PageTable.KernelTable.Map(kmap.Start, kmap.Start, PhysicalPageManager.GetAllocatorByAddr(kmap.Start), flush: true);
            PageTable.KernelTable.SetWritable(kmap.Start, kmap.Size);
            kmap.Clear();
            return new MemoryRegion(kmap.Start, kmap.Size);
        }

        protected static MemoryRegion AllocRawMemoryVirt(uint size)
        {
            var kmap = VirtualPageManager.AllocateRegion(size);
            kmap.Clear();
            return kmap;
        }

        /// <summary>
        /// Setups the free memory.
        /// </summary>
        protected override unsafe void SetupFreeMemory()
        {
            for (uint i = 0; i < _TotalPages; i++)
                PageArray[i].Status = PageStatus.Free;

            //SetInitialPageStatus(&KernelMemoryMapManager.Header->SystemUsable, PageStatus.Free);
            SetInitialPageStatus(&KernelMemoryMapManager.Header->Used, PageStatus.Used);
            SetInitialPageStatus(&KernelMemoryMapManager.Header->KernelReserved, PageStatus.Used);
        }

        private void SetInitialPageStatus(KernelMemoryMapArray* maps, PageStatus status)
        {
            for (var i = 0; i < maps->Count; i++)
            {
                var map = maps->Items[i];
                if (map.Start >= BootInfo.Header->InstalledPhysicalMemory)
                    continue;

                if ((map.AddressSpaceKind & AddressSpaceKind.Virtual) == 0)
                    continue;

                var mapPages = KMath.DivCeil(map.Size, 4096);
                var fistPageNum = KMath.DivFloor(map.Start, 4096);
                KernelMessage.WriteLine("Mark Pages from {0:X8}, Size {1:X8}, Type {2}, FirstPage {3}, Pages {4}, Status {5}", map.Start, map.Size, (uint)map.Type, (uint)fistPageNum, mapPages, (uint)status);

                for (var p = fistPageNum; p < fistPageNum + mapPages; p++)
                {
                    var addr = p * 4096;
                    if (!Region.Contains(addr))
                        continue;

                    if (addr >= BootInfo.Header->InstalledPhysicalMemory)
                    {
                        KernelMessage.WriteLine("addr >= BootInfo.Header->InstalledPhysicalMemory");
                        break;
                    }
                    var page = GetPageByNum(p);
                    Assert.IsSet(page, "page == null");
                    page->Status = status;
                }
            }
        }

    }

}
