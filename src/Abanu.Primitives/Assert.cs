﻿// This file is part of Abanu, an Operating System written in C#. Web: https://www.abanu.org
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Abanu
{
    public delegate void ExceptionHandler(string message);

    public static class Assert
    {

        public delegate void DAssertErrorHandler(string errorMessage, uint arg1 = 0, uint arg2 = 0, uint arg3 = 0);

        private static DAssertErrorHandler ErrorHandler;

        public static void Setup(DAssertErrorHandler errorHandler)
        {
            ErrorHandler = errorHandler;
        }

        private static void AssertError(string message, uint arg1 = 0, uint arg2 = 0, uint arg3 = 0)
        {
            if (ErrorHandler == null)
            {
                throw new Exception(message);
            }
            ErrorHandler(message, arg1, arg2, arg3);
        }

        [Conditional("DEBUG")]
        public static void InRange(uint value, uint length)
        {
            if (value >= length)
                AssertError("Out of Range");
        }

        [Conditional("DEBUG")]
        public static void True(bool condition)
        {
            if (!condition)
                AssertError("Assert.True failed");
        }

        [Conditional("DEBUG")]
        public static void True(bool condition, string userMessage, uint arg1 = 0, uint arg2 = 0, uint arg3 = 0)
        {
            if (!condition)
                AssertError(userMessage, arg1, arg2, arg3);
        }

        [Conditional("DEBUG")]
        public static void False(bool condition)
        {
            if (condition)
                AssertError("Assert.False failed");
        }

        [Conditional("DEBUG")]
        public static void False(bool condition, string userMessage)
        {
            if (condition)
                AssertError(userMessage);
        }

        [Conditional("DEBUG")]
        public static void IsSet(uint value, string userMessage)
        {
            if (value == 0)
                AssertError(userMessage);
        }

        [Conditional("DEBUG")]
        public static void IsSet(IntPtr value, string userMessage)
        {
            if (value == IntPtr.Zero)
                AssertError(userMessage);
        }

        [Conditional("DEBUG")]
        public static void IsSet(Addr value, string userMessage)
        {
            if (value == Addr.Zero)
                AssertError(userMessage);
        }

    }
}
