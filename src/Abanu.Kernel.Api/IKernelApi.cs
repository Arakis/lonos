﻿// This file is part of Abanu, an Operating System written in C#. Web: https://www.abanu.org
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;

namespace Abanu.Kernel.Core
{

    public interface IKernelApi
    {
        /// <summary>
        /// fwrite
        /// </summary>
        int FileWrite(IntPtr ptr, USize elementSize, USize elements, FileHandle stream);

        /// <summary>
        /// fputs
        /// </summary>
        int FileWrite(NullTerminatedString str, FileHandle stream);

        /// <summary>
        /// write
        /// </summary>
        /// <returns>The write.</returns>
        SSize FileWrite(FileHandle file, IntPtr buf, USize count);
    }

}
