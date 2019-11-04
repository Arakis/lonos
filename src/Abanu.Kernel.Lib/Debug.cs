﻿// This file is part of Abanu, an Operating System written in C#. Web: https://www.abanu.org
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using Mosa.Runtime;
using Mosa.Runtime.Plug;
using Mosa.Runtime.x86;

namespace Abanu.Kernel.Core
{
    public static unsafe class Debug
    {

        public static void Setup()
        {
        }

        public static void Break()
        {
            KernelMessage.Write("<BREAK>");
            while (true)
            {
                Native.Nop();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Nop()
        {
            Native.Nop();
        }

    }
}
