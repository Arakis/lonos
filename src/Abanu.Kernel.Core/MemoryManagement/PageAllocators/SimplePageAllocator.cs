﻿// This file is part of Abanu, an Operating System written in C#. Web: https://www.abanu.org
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abanu.Kernel.Core.Diagnostics;

namespace Abanu.Kernel.Core.MemoryManagement.PageAllocators
{
    public unsafe class SimplePageAllocator : IPageFrameAllocator
    {

        private Page* Pages;
        private uint FirstPageNum;

        private ulong _Requests;
        public ulong Requests => _Requests;

        private ulong _Releases;
        public ulong Releases => _Releases;

        private string _DebugName;
        public string DebugName
        {
            get { return _DebugName; }
            set { _DebugName = value; }
        }

        public void Initialize(MemoryRegion region, Page* pages, AddressSpaceKind addressSpaceKind)
        {
            _Requests = 0;
            _Releases = 0;

            KernelMessage.WriteLine("Init SimplePageAllocator");
            AddressSpaceKind = addressSpaceKind;
            _Region = region;
            Pages = pages;
            FirstPageNum = region.Start / 4096;
            _FreePages = region.Size / 4096;
            _TotalPages = region.Size / 4096;
            var addr = region.Start;
            for (var i = 0; i < _TotalPages; i++)
            {
                Pages[i].Address = addr;
                addr += 4096;
            }
        }

        private uint _TotalPages;
        public uint TotalPages => _TotalPages;

        private uint _FreePages;
        public uint FreePages => _FreePages;

        private MemoryRegion _Region;
        public MemoryRegion Region => _Region;

        public AddressSpaceKind AddressSpaceKind { get; private set; }

        public uint MaxPagesPerAllocation => uint.MaxValue / 4096;
        public uint CriticalLowPages => 1000;

        public unsafe Page* AllocatePage(AllocatePageOptions options = default)
        {
            return AllocatePages(1, options);
        }

        public unsafe Page* AllocatePages(uint pages, AllocatePageOptions options = default)
        {
            if (pages > _FreePages)
            {
                Panic.Error("Out of Memory");
                return null;
            }

            Page* page;
            if (AddressSpaceKind == AddressSpaceKind.Virtual || options.Continuous)
                page = AllocatePagesContinuous(pages, options);
            else
                page = AllocatePagesNormal(pages, options);

            if (page == null)
            {
                KernelMessage.WriteLine("DebugName: {0}", DebugName);
                KernelMessage.WriteLine("Free pages: {0:X8}, Requested: {1:X8}", FreePages, pages);
                Panic.Error("Out of Memory");
            }

            KernelMessage.Path(DebugName, "SimpleAlloc: Request {0} Pages, Addr {1:X8}", pages, GetAddress(page));

            return page;
        }

        private unsafe Page* AllocatePagesNormal(uint pages, AllocatePageOptions options = default)
        {
            Page* prevHead = null;
            Page* firstHead = null;
            var pagesFound = 0;
            for (var i = 0; i < _TotalPages; i++)
            {
                var page = &Pages[i];
                if (!InUse(page))
                {
                    if (firstHead == null)
                        firstHead = page;

                    if (prevHead != null)
                        SetNext(prevHead, page);
                    SetInUse(page);

                    pagesFound++;
                    _FreePages--;

                    if (pagesFound >= pages)
                        return firstHead;

                    prevHead = page;
                }
            }

            return null;
        }

        private unsafe Page* AllocatePagesContinuous(uint pages, AllocatePageOptions options = default)
        {
            for (var i = 0; i < _TotalPages; i++)
            {
                if (i + pages >= _TotalPages)
                    return null;

                var head = &Pages[i];
                if (!InUse(head))
                {
                    var foundContinuous = true;
                    for (var n = 1; n < pages; n++)
                    {
                        if (InUse(&Pages[++i]))
                        {
                            foundContinuous = false;
                            break;
                        }
                    }

                    if (!foundContinuous)
                        continue;

                    var p = head;
                    for (var n = 0; n < pages; n++)
                    {
                        SetInUse(p);

                        if (n == pages - 1)
                            SetNext(p, null);
                        else
                            SetNext(p, p + 1);

                        p++;
                    }

                    _FreePages -= pages;

                    return head;
                }
            }

            return null;
        }

        private static bool InUse(Page* page) => (page->Flags & (1u << 1)) != 0;
        private static void SetInUse(Page* page) => page->Flags |= 1u << 1;
        private static void ClearInUse(Page* page) => page->Flags &= ~(1u << 1);

        // Don't be confused: Reuse of FirstPage field of buddy allocator just to save space
        private static void SetNext(Page* page, Page* next) => page->FirstPage = next;

        public unsafe bool ContainsPage(Page* page)
        {
            return Region.Contains(page->Address);
        }

        public unsafe void Free(Page* page)
        {
            var next = page;
            while (next != null)
            {
                SetNext(page, null);
                ClearInUse(page);
                _FreePages++;
                next = NextCompoundPage(page);
            }
        }

        public unsafe uint GetAddress(Page* page)
        {
            return page->Address;
        }

        public unsafe Page* GetPageByAddress(Addr addr)
        {
            return GetPageByNum(addr / 4096);
        }

        public unsafe Page* GetPageByIndex(uint pageIndex)
        {
            if (pageIndex >= _TotalPages)
                return null;

            return &Pages[pageIndex];
        }

        public unsafe Page* GetPageByNum(uint pageNum)
        {
            return GetPageByIndex(pageNum - FirstPageNum);
        }

        public unsafe uint GetPageIndex(Page* page)
        {
            return GetPageNum(page) - FirstPageNum;
        }

        public uint GetPageIndex(Addr addr)
        {
            return (addr / 4096) - FirstPageNum;
        }

        public unsafe uint GetPageNum(Page* page)
        {
            return page->Address / 4096;
        }

        public unsafe Page* NextPage(Page* page)
        {
            return GetPageByIndex(GetPageIndex(page) + 1);
        }

        public Page* NextCompoundPage(Page* page)
        {
            //KernelMessage.WriteLine("NextCompoundPage: {0:X8}, {1:X8}", GetAddress(page), (uint)page->FirstPage);
            return page->FirstPage;
        }

        public void SetTraceOptions(PageFrameAllocatorTraceOptions options)
        {
        }

    }

}
