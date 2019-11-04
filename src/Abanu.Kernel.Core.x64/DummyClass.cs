﻿// This file is part of Abanu, an Operating System written in C#. Web: https://www.abanu.org
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable SA1300 // Element should begin with upper-case letter
namespace Abanu.Kernel.Core.x64
#pragma warning restore SA1300 // Element should begin with upper-case letter
{
    public static class DummyClass
    {
        public static void DummyCall()
        {
            Core.DummyClass.DummyCall();
            var t = typeof(Mosa.Runtime.GC);
            var t2 = typeof(Mosa.Plug.Korlib.System.Threading.x64.InterlockedPlug);
            var t3 = typeof(Mosa.Runtime.x64.Internal);
        }
    }
}
