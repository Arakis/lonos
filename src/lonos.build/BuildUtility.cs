﻿// Copyright (c) MOSA Project. Licensed under the New BSD License.

using System;
using System.Text.RegularExpressions;
using System.IO;

namespace lonos.Build
{
    public static class BuildUtility {
        public static string GetEnv(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(value))
            {
                switch (name)
                {
                    case "LONOS_PROJDIR":
                        value = Path.GetDirectoryName(Path.GetDirectoryName(new Uri(typeof(Program).Assembly.Location).AbsolutePath));
                        break;
                    case "LONOS_OSDIR":
                        value = "${LONOS_PROJDIR}/os";
                        break;
                    case "LONOS_NATIVE_FILES":
                        value = "${LONOS_PROJDIR}/bin/x86/lonos.native.o";
                        break;
                    case "LONOS_BOOTLOADER_EXE":
                        value = "${LONOS_PROJDIR}/bin/lonos.os.loader.x86.exe";
                        break;
                    case "LONOS_EXE":
                        value = "${LONOS_PROJDIR}/bin/lonos.os.core.x86.exe";
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
                }
            }

            var regex = new Regex(@"\$\{(\w+)\}", RegexOptions.RightToLeft);

            if (string.IsNullOrEmpty(value))
                value = name;

            if (!string.IsNullOrEmpty(value))
                foreach (Match m in regex.Matches(value))
                    value = value.Replace(m.Value, GetEnv(m.Groups[1].Value));
            return value;
        }
    }
}
