﻿// This file is part of Lonos Project, an Operating System written in C#. Web: https://www.lonos.io
// Licensed under the GNU 2.0 license. See LICENSE.txt file in the project root for full license information.

namespace Lonos.Kernel
{

    public enum SysCallTarget
    {
        Unset = 0,

        Interrupt = 1,

        RequestMemory = 20,
        ServiceReturn = 21,
        ServiceFunc1 = 22,
        RequestMessageBuffer = 23,
        WriteDebugMessage = 24,
        GetProcessIDForCommand = 25,
        GetPhysicalMemory = 26,
        TranslateVirtualToPhysicalAddress = 27,
        WriteDebugChar = 28,
        SetThreadPriority = 29,

        OpenFile = 30,
        ReadFile = 31,
        WriteFile = 32,
        CreateFifo = 33,

        CreateMemoryProcess = 40,

        HostCommunication_OpenFile = 41,
        HostCommunication_ReadFile = 42,
        HostCommunication_WriteFile = 43,
        HostCommunication_CreateProcess = 44,

        SetServiceStatus = 45,
        RegisterService = 46,
        RegisterInterrupt = 47,
    }

}
