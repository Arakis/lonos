﻿// This file is part of Abanu, an Operating System written in C#. Web: https://www.abanu.org
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

namespace Abanu.Kernel
{

    public enum SysCallTarget
    {
        Unset = 0,

        ServiceReturn = 1,
        Interrupt = 2,

        GetProcessIDForCommand = 10,

        RequestMessageBuffer = 20,
        RequestMemory = 21,
        GetPhysicalMemory = 22,
        TranslateVirtualToPhysicalAddress = 23,
        Sbrk = 24,

        OpenFile = 30,
        ReadFile = 31,
        WriteFile = 32,
        CreateFifo = 33,
        GetFileLength = 34,
        FStat = 35,
        CreateStandartInputOutput = 36,
        SetStandartInputOutput = 37,

        CreateMemoryProcess = 40, // called from Service. Hooked in Kernel
        SetThreadPriority = 41,
        ThreadSleep = 42,
        GetProcessByName = 43,
        KillProcess = 44,
        GetElfSectionsAddress = 45,
        GetFramebufferInfo = 46,
        GetCurrentProcessID = 47,
        GetCurrentThreadID = 48,
        SetThreadStorageSegmentBase = 49,

        SetServiceStatus = 50,
        RegisterService = 51,
        RegisterInterrupt = 52,

        CreateProcessFromFile = 55, // called from app. Hooked in Service
        StartProcess = 56, // called from app or service. Hooked in Kernel.

        WriteDebugMessage = 60,
        WriteDebugChar = 61,

        GetRemoteProcessID = 70,
        GetRemoteThreadID = 71,

        //ServiceFunc1 = 100,

        HostCommunication_OpenFile = 110,
        HostCommunication_ReadFile = 111,
        HostCommunication_WriteFile = 112,
        HostCommunication_CreateProcess = 113,

        TmpDebug = 120,

        // TODO: No hardcoded values
        Tmp_DisplayServer_CreateWindow = 131,
        Tmp_DisplayServer_FlushWindow = 132,

    }

}
