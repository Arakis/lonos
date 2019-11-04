﻿// This file is part of Abanu, an Operating System written in C#. Web: https://www.abanu.org
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using Mosa.Runtime.Plug;
using Mosa.Runtime.x86;

namespace Abanu.Kernel.Loader
{
    /// <summary>
    /// GDT
    /// </summary>
    public static class SSE
    {
        [Plug("Mosa.Runtime.StartUp::InitalizeProcessor1")]
        //[Plug("Mosa.Runtime.StartUp::KernalInitialization")]
        public static void Setup()
        {
            Native.SetCR0(Native.GetCR0() & 0xFFFB | 0x2);
            Native.SetCR4(Native.GetCR4() | 0x600);
        }
    }
}
