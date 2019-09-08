﻿// Copyright (c) MOSA Project. Licensed under the New BSD License.

using Mosa.Compiler.Framework.Linker;
using Mosa.Compiler.MosaTypeSystem;
using Mosa.Utility.BootImage;
using Mosa.Utility.Launcher;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Threading;

using Mosa.Compiler.Framework.Linker.Elf;
using Mosa.Compiler.Common;

using lonos.Kernel.Core;

namespace lonos.Build
{
    public class LonosBuilder_Loader : LonosBuilder
    {

        public LonosBuilder_Loader(string inputAssembly) : base(inputAssembly)
        {
        }

        public override void Configure()
        {
            InputAssembly = BuildUtility.GetEnv("LONOS_BOOTLOADER_EXE");

            Options = new LauncherOptions()
            {
                EnableSSA = true,
                EnableIROptimizations = true,
                EnableSparseConditionalConstantPropagation = true,
                EnableInlinedMethods = true,
                EnableLongExpansion = false, // Compiler commit 2e23a85: If true, the loader is not able to display the section names
                EnableValueNumbering = true,
                TwoPassOptimizations = true,
                //EnableBitTracker = true,

                Emulator = EmulatorType.Bochs,
                ImageFormat = ImageFormat.IMG,
                //BootFormat = BootFormat.Multiboot_0_7,
                MultiBootV1 = true,
                PlatformType = PlatformType.x86,
                LinkerFormatType = LinkerFormatType.Elf32,
                EmulatorMemoryInMB = 128,
                DestinationDirectory = BuildUtility.GetEnv("LONOS_OSDIR"),
                FileSystem = FileSystem.FAT16,

                //UseMultiThreadingCompiler = false,
                EnableMultiThreading = false,

                InlinedIRMaximum = 12,
                BootLoader = BootLoader.Syslinux_3_72,
                VBEVideo = false,
                BaseAddress = Address.LoaderBasePhys,
                //EmitRelocations = false,
                //EmitSymbols = false,
                //Emitx86IRQMethods = false,
                //SerialConnectionOption = SerialConnectionOption.Pipe,
                // EmitAllSymbols = true,
                // EnableQemuGDB = true,
                ExitOnLaunch = true,
                GenerateNASMFile = false,
                GenerateASMFile = true,
                GenerateMapFile = true,
                GenerateDebugFile = false,
                PlugKorlib = true,
                HuntForCorLib = true
            };

            Options.VBEVideo = true;
            Options.EmitAllSymbols = true;
            //Options.EnableMethodScanner = true;

            Section sect = null;
            Options.CreateExtraSections = () =>
            {
                return new List<Section>
                {
                    new Section
                    {
                        Name = "native",
                        Type = SectionType.ProgBits,
                        AddressAlignment = 0x1000,
                        EmitMethod = (section, writer) =>
                        {
                            var data = File.ReadAllBytes(BuildUtility.GetEnv("LONOS_NATIVE_FILES"));
                            writer.Write(data);
                            section.Size = (uint)data.Length;
                        }
                    }
                };
            };

            Options.CreateExtraProgramHeaders = () =>
            {
                return new List<ProgramHeader>
                 {
                    // ELF Header. Reusing existing Region in File.
                    // This is allowed (overlapping Sections)
                    new ProgramHeader
                    {
                        Alignment = 0x1000,
                        Offset =   0x12345678, // Will be replaced in Link Disk stage
                        FileSize = 0x12345678, // Will be replaced in Link Disk stage
                        MemorySize = 0x12345678,
                        PhysicalAddress = Address.OriginalKernelElfSection, //Multiboot will load section here
                        VirtualAddress = Address.OriginalKernelElfSection,
                        Type = ProgramHeaderType.Load,
                        Flags = ProgramHeaderFlags.Read
                    }

                 };
            };
        }
    }
}
