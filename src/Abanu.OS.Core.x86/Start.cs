﻿// This file is part of Abanu, an Operating System written in C#. Web: https://www.abanu.org
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using Abanu.Kernel.Core;

#pragma warning disable SA1300 // Element should begin with upper-case letter
namespace Abanu.OS.Core.x86
#pragma warning restore SA1300 // Element should begin with upper-case letter
{
    public static class Start
    {
        public static void Main()
        {
            Kernel.Core.KernelStart.Main();
            Kernel.Core.x86.DummyClass.DummyCall();
            while (true)
            {
            }
        }
    }
}
