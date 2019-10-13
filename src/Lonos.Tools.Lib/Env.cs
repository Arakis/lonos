﻿// This file is part of Lonos Project, an Operating System written in C#. Web: https://www.lonos.io
// Licensed under the GNU 2.0 license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Lonos.Tools
{

    public static class Env
    {

        public static CultureInfo LocaleInvariant = CultureInfo.InvariantCulture;

        public static void Set(string name, string value)
        {
            Vars.Remove(name);
            Vars.Add(name, value);
        }

        private static Dictionary<string, string> Vars = new Dictionary<string, string>();

        public static string Get(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (value == null)
            {
                switch (name)
                {
                    case "LONOS_PROJDIR":
                        value = Path.GetDirectoryName(Path.GetDirectoryName(new Uri(typeof(Env).Assembly.Location).AbsolutePath));
                        if (!File.Exists(Path.Combine(value, "lonosctl")))
                            value = Path.GetDirectoryName(value);
                        if (!File.Exists(Path.Combine(value, "lonosctl")))
                            value = Path.GetDirectoryName(value);
                        if (!File.Exists(Path.Combine(value, "lonosctl")))
                            value = Path.GetDirectoryName(value);
                        if (!File.Exists(Path.Combine(value, "lonosctl")))
                            value = null;
                        break;
                    case "LONOS_ARCH":
                        value = "x86";
                        break;
                    case "LONOS_BINDIR":
                        value = "${LONOS_PROJDIR}/bin";
                        break;
                    case "LONOS_OSDIR":
                        value = "${LONOS_PROJDIR}/os";
                        break;
                    case "LONOS_NATIVE_FILES":
                        value = "${LONOS_PROJDIR}/bin/${LONOS_ARCH}/Lonos.Native.o";
                        break;
                    case "LONOS_BOOTLOADER_EXE":
                        value = "${LONOS_PROJDIR}/bin/Lonos.OS.Loader.${LONOS_ARCH}.exe";
                        break;
                    case "LONOS_EXE":
                        value = "${LONOS_PROJDIR}/bin/Lonos.OS.Core.${LONOS_ARCH}.exe";
                        break;
                    case "LONOS_LOGDIR":
                        value = "${LONOS_PROJDIR}/logs";
                        break;
                    case "LONOS_ISODIR":
                        value = "${LONOS_PROJDIR}/iso";
                        break;
                    case "LONOS_TOOLSDIR":
                        value = "${LONOS_PROJDIR}/tools";
                        break;
                    case "MOSA_PROJDIR":
                        value = "${LONOS_PROJDIR}/external/MOSA-Project";
                        break;
                    case "MOSA_TOOLSDIR":
                        value = "${MOSA_PROJDIR}/tools";
                        break;
                    case "qemu":
                        value = "${MOSA_TOOLSDIR}/qemu/qemu-system-x86_64.exe";
                        break;
                    case "gdb":
                        value = @"gdb.exe";
                        break;
                    case "nasm":
                        value = "${MOSA_TOOLSDIR}/nasm/nasm.exe";
                        break;
                }

                if (Vars.ContainsKey(name))
                    value = Vars[name];
            }

            var regex = new Regex(@"\$\{(\w+)\}", RegexOptions.RightToLeft);

            if (value == null)
                value = name;

            if (value != null)
                foreach (Match m in regex.Matches(value))
                    value = value.Replace(m.Value, Get(m.Groups[1].Value));
            return value;
        }
    }
}
