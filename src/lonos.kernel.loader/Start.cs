﻿using System;
using Mosa.Runtime;
using Mosa.Kernel.x86;
using Mosa.Runtime.Plug;
using Mosa.Runtime.x86;

namespace lonos.kernel.core
{

    internal unsafe static class Start
    {

        public static void Main()
        {
            //Native.Nop();
            //Native.Nop();
            //jump();

            //Native.Nop();
            //Native.Nop();

            Serial.SetupPort(Serial.COM1);
            KernelMessage.WriteLine("hello");

            Multiboot.Setup();

            // Detect environment (Memory Maps, Video Mode, etc.)
            //Multiboot.Setup();


            // Setup Global Descriptor Table
            //GDT.Setup();

            //PageTable.Setup();



            SetupOriginalKernelElf();
            DumpElfInfo();
            SetupKernelSection();
            SetupBootInfo();
            SetupVideoInfo();
            SetupMemoryMap();
            uint kernelEntry = Address.KernelBaseVirt + 0x30;
            Native.Call(kernelEntry);
            Debug.Break();
        }

        private static ElfHelper OriginalKernelElf;

        private static void SetupOriginalKernelElf()
        {
            uint kernelElfHeaderAddr = Address.OriginalKernelElfSection;
            var kernelElfHeader = (ElfHeader*)kernelElfHeaderAddr;
            OriginalKernelElf = new ElfHelper
            {
                PhyOffset = kernelElfHeaderAddr,
                SectionHeaderArray = (ElfSectionHeader*)(kernelElfHeaderAddr + kernelElfHeader->ShOff),
                SectionHeaderCount = kernelElfHeader->ShNum,
                StringTableSectionHeaderIndex = kernelElfHeader->ShStrNdx
            };
            OriginalKernelElf.Init();
        }

        unsafe static void SetupKernelSection()
        {
            // TODO: Respect section progream header adresses.
            // Currently, we can make a raw copy of ELF file
            Memory.Copy4(Address.OriginalKernelElfSection, Address.KernelElfSection, OriginalKernelElf.TotalFileSize);
        }

        static BootInfoHeader* BootInfo;

        static void SetupBootInfo()
        {
            BootInfo = (BootInfoHeader*)Address.KernelBootInfo;
            BootInfo->Magic = lonos.kernel.core.BootInfoHeader.BootInfoMagic;
            BootInfo->HeapStart = KMath.AlignValueCeil(Address.OriginalKernelElfSection + OriginalKernelElf.TotalFileSize, 0x1000);
            BootInfo->HeapSize = 0;
        }

        static void SetupVideoInfo()
        {
            BootInfo->VBEPresent = Multiboot.VBEPresent;
            BootInfo->VBEMode = Multiboot.VBEMode;

            BootInfo->FbInfo = new BootInfoFramebufferInfo();
            BootInfo->FbInfo.FbAddr = Multiboot.multiBootInfo->FbAddr;
            BootInfo->FbInfo.FbPitch = Multiboot.multiBootInfo->FbPitch;
            BootInfo->FbInfo.FbWidth = Multiboot.multiBootInfo->FbWidth;
            BootInfo->FbInfo.FbHeight = Multiboot.multiBootInfo->FbHeight;
            BootInfo->FbInfo.FbBpp = Multiboot.multiBootInfo->FbBpp;
            BootInfo->FbInfo.FbType = Multiboot.multiBootInfo->FbType;
            BootInfo->FbInfo.ColorInfo = Multiboot.multiBootInfo->ColorInfo;
        }

        static void SetupMemoryMap()
        {
            uint customMaps = 4;
            var mbMapCount = Multiboot.MemoryMapCount;
            BootInfo->MemoryMapLength = mbMapCount + customMaps;
            BootInfo->MemoryMapArray = (BootInfoMemory*)MallocBootInfoData((USize)(sizeof(MultiBootMemoryMap) * mbMapCount));

            for (uint i = 0; i < mbMapCount; i++)
            {
                BootInfo->MemoryMapArray[i].Start = Multiboot.GetMemoryMapBase(i);
                BootInfo->MemoryMapArray[i].Size = Multiboot.GetMemoryMapLength(i);
                var memType = BootInfoMemoryType.Reserved;
                var type = (BIOSMemoryMapType)Multiboot.GetMemoryMapType(i);

                switch (type)
                {
                    case BIOSMemoryMapType.Usable:
                        memType = BootInfoMemoryType.Usable;
                        break;
                    case BIOSMemoryMapType.Reserved:
                        memType = BootInfoMemoryType.Reserved;
                        break;
                    case BIOSMemoryMapType.ACPI_Relaimable:
                        memType = BootInfoMemoryType.ACPI_Relaimable;
                        break;
                    case BIOSMemoryMapType.ACPI_NVS_Memory:
                        memType = BootInfoMemoryType.ACPI_NVS_Memory;
                        break;
                    case BIOSMemoryMapType.BadMemory:
                        memType = BootInfoMemoryType.BadMemory;
                        break;
                    default:
                        memType = BootInfoMemoryType.Unknown;
                        break;
                }
                BootInfo->MemoryMapArray[i].Type = memType;
            }

            var idx = mbMapCount + 0;
            BootInfo->MemoryMapArray[idx].Start = Address.OriginalKernelElfSection;
            BootInfo->MemoryMapArray[idx].Size = KMath.AlignValueCeil(OriginalKernelElf.TotalFileSize, 0x1000);
            BootInfo->MemoryMapArray[idx].Type = BootInfoMemoryType.OriginalKernelElfImage;

            idx++;
            BootInfo->MemoryMapArray[idx].Start = Address.KernelElfSection;
            BootInfo->MemoryMapArray[idx].Size = KMath.AlignValueCeil(OriginalKernelElf.TotalFileSize, 0x1000);
            BootInfo->MemoryMapArray[idx].Type = BootInfoMemoryType.KernelElf;

            idx++;
            BootInfo->MemoryMapArray[idx].Start = Address.KernelBootInfo;
            BootInfo->MemoryMapArray[idx].Size = 0x1000;
            BootInfo->MemoryMapArray[idx].Type = BootInfoMemoryType.BootInfoHeader;

            idx++;
            BootInfo->MemoryMapArray[idx].Start = BootInfo->HeapStart;
            BootInfo->MemoryMapArray[idx].Size = 0x1000; //TODO: Recaluclate after Setup all Infos
            BootInfo->MemoryMapArray[idx].Type = BootInfoMemoryType.BootInfoHeap;
        }

        static Addr MallocBootInfoData(USize size)
        {
            var ret = BootInfo->HeapStart + BootInfo->HeapSize;
            BootInfo->HeapSize += size;
            return ret;
        }

        public unsafe static void DumpElfInfo()
        {
            var secArray = OriginalKernelElf.SectionHeaderArray;
            var secLength = OriginalKernelElf.SectionHeaderCount;

            KernelMessage.WriteLine("CNT");
            KernelMessage.WriteLine(secLength);

            for (uint i = 0; i < secLength; i++)
            {
                var sec = OriginalKernelElf.GetSectionHeader(i);
                var name = OriginalKernelElf.GeSectionName(sec);
                var sb = new StringBuffer(name);
                KernelMessage.WriteLine(sb);
            }
        }

        static void Dummy()
        {
            //This is a dummy call, that get never executed.
            //Its requied, because we need a real reference to Mosa.Runtime.x86
            //Without that, the .NET compiler will optimize that reference away
            //if its nowhere used. Than the Compiler dosnt know about that Refernce
            //and the Compilation will fail
            Mosa.Runtime.x86.Internal.GetStackFrame(0);
        }

    }
}
