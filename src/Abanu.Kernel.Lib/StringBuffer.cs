﻿// This file is part of Abanu, an Operating System written in C#. Web: https://www.abanu.org
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Abanu.Kernel.Core
{
    /// <summary>
    /// Represents a string as struct, so it can used before memory and runtime initialization.
    /// Use only where needed. Do not increase the struct size more as needed. A good limit would be the maximum horizontal text resolution.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct StringBuffer
    {
        private uint _length;

        public const int MaxLength = 132;
        public const int EntrySize = (MaxLength * 2) + 4;

        private unsafe fixed byte _chars[MaxLength];

        public static unsafe StringBuffer CreateFromNullTerminatedString(uint start)
        {
            return CreateFromNullTerminatedString((byte*)start);
        }

        public static unsafe StringBuffer CreateFromNullTerminatedString(byte* start)
        {
            var buf = new StringBuffer();
            while (*start != 0)
            {
                buf.Append((char)*start++);
            }
            return buf;
        }

        /// <summary>
        /// Access a char at a specific index
        /// </summary>
        public unsafe char this[int index]
        {
            get
            {
                if (index < 0 || index >= Length) //TODO: Error
                    return '\x0';

                fixed (byte* ptr = _chars)
                    return (char)ptr[index];
            }

            set
            {
                if (index < 0 || index >= Length) //TODO: Error
                    return;
                fixed (byte* ptr = _chars)
                    ptr[index] = (byte)value;
            }
        }

        public unsafe char this[uint index]
        {
            get
            {
                if (index >= Length) //TODO: Error
                    return '\x0';
                fixed (byte* ptr = _chars)
                    return (char)ptr[index];
            }

            set
            {
                if (index >= Length) //TODO: Error
                    return;
                fixed (byte* ptr = _chars)
                    ptr[index] = (byte)value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _length = 0;
        }

        /// <summary>
        /// Overwrite the current value with a new one
        /// </summary>
        public void Set(string value)
        {
            Clear();
            //if (value == null)
            //  isSet = 0;
            //else
            Append(value);
        }

        //public bool IsNull
        //{
        //  get { return isSet == 0; }
        //}

        #region Constructor

        public StringBuffer(string value)
            : this()
        {
            Append(value);
        }

        public StringBuffer(byte value)
            : this()
        {
            Append(value);
        }

        public StringBuffer(int value)
            : this()
        {
            Append(value);
        }

        public StringBuffer(int value, string format)
            : this()
        {
            Append(value, format);
        }

        public StringBuffer(uint value)
            : this()
        {
            Append(value);
        }

        public StringBuffer(uint value, string format)
            : this()
        {
            Append(value, format);
        }

        public unsafe StringBuffer(NullTerminatedString* value)
            : this()
        {
            Append(value);
        }

        #endregion Constructor

        #region Append

        /// <summary>
        /// Appends a string
        /// </summary>
        public void Append(string value)
        {
            if (value == null)
                return;

            for (var i = 0; i < value.Length; i++)
                Append(value[i]);
        }

        /// <summary>
        /// Appends a string
        /// </summary>
        public unsafe void Append(NullTerminatedString* value)
        {
            var len = value->GetLength();
            for (var i = 0; i < len; i++)
                Append((char)value->Bytes[i]);
        }

        public void AppendSubString(string value, int start)
        {
            if (value == null)
                return;
            AppendSubString(value, start, value.Length - start);
        }

        public void AppendSubString(string value, int start, int length)
        {
            if (value == null)
                return;
            for (var i = 0; i < length; i++)
                Append(value[i + start]);
        }

        public void Append(StringBuffer value)
        {
            if (value._length == 0)
                return;

            for (var i = 0; i < value.Length; i++)
                Append(value[i]);
        }

        public void Append(StringBuffer value, uint start)
        {
            if (value._length == 0)
                return;
            Append(value, start, value.Length - start);
        }

        public void Append(StringBuffer value, uint start, uint length)
        {
            if (value._length == 0)
                return;
            for (uint i = 0; i < length; i++)
                Append(value[i + start]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Append(char value)
        {
            if (_length + 1 >= MaxLength)
            {
                //TODO: Error
                return;
            }
            //isSet = 1;
            _length++;
            this[_length - 1] = value;
        }

        public void Append(uint value)
        {
            Append(value, false, false);
        }

        public void Append(ulong value)
        {
            Append(value, false, false);
        }

        public void Append(long value)
        {
            Append((ulong)value, true, false);
        }

        public void Append(int value)
        {
            Append((uint)value, true, false);
        }

        /// <summary>
        /// Appends a number to the string. Use format to output as Hex.
        /// </summary>
        public void Append(uint value, string format)
        {
            var sb = new StringBuffer(format);
            Append(value, sb);
        }

        private void Append(Argument value, StringBuffer format)
        {
            switch (value.Type)
            {
                case ArgumentType.UInt:
                    Append(value.UInt, format);
                    break;
                case ArgumentType.Int:
                    Append(value.UInt, format); // TODO
                    break;
                case ArgumentType.String:
                    Append(value.String);
                    break;
                default:
                    Append("Unknown ArgumentType");
                    break;
            }
        }

        public void Append(uint value, StringBuffer format)
        {
            if (format._length == 0)
            {
                Append(value, 10, -1);
                return;
            }

            if (format._length >= 1 && format[0] == 'X')
            {
                if (format._length == 1)
                {
                    Append(value, 16, -1);
                    return;
                }
                if (format._length == 2)
                {
                    Append(value, 16, CharDigitToInt(format[1]));
                    return;
                }
            }

            if (format._length >= 1 && format[0] == 'D')
            {
                if (format._length == 1)
                {
                    Append(value, 10, -1);
                    return;
                }
                if (format._length == 2)
                {
                    Append(value, 10, CharDigitToInt(format[1]));
                    return;
                }
            }

            Append("UNSUPPORTED FORMAT: ");
            Append(format);
        }

        private static int CharDigitToInt(char digit)
        {
            var num = ((byte)digit) - ((byte)'0');
            if (num < 0 || num > 9)
                return -1;
            return num;
        }

        private static uint CharDigitToUInt(char digit)
        {
            var num = ((byte)digit) - ((byte)'0');
            if (num < 0 || num > 9)
                return uint.MaxValue;
            return (uint)num;
        }

        /// <summary>
        /// Appends a number to the string. Use format to output as Hex.
        /// </summary>
        public void Append(int value, string format)
        {
            var u = (uint)value;
            Append(u, true, true);
        }

        private unsafe void Append(uint value, bool signed, bool hex)
        {
            int offset = 0;

            uint uvalue = (uint)value;
            ushort divisor = hex ? (ushort)16 : (ushort)10;
            uint len = 0;
            uint count = 0;
            uint temp;
            bool negative = false;

            if (value < 0 && !hex && signed)
            {
                count++;
                uvalue = (uint)-value;
                negative = true;
            }

            temp = uvalue;

            do
            {
                temp /= divisor;
                count++;
            }
            while (temp != 0);

            byte* first;
            fixed (byte* ptr = _chars)
            {
                first = ptr + this._length;
            }

            len = count;
            Length += len;

            if (negative)
            {
                *(first + offset) = (byte)'-';
                offset++;
                count--;
            }

            for (int i = 0; i < count; i++)
            {
                uint remainder = uvalue % divisor;

                if (remainder < 10)
                    *(first + offset + count - 1 - i) = (byte)('0' + remainder);
                else
                    *(first + offset + count - 1 - i) = (byte)('A' + remainder - 10);

                uvalue /= divisor;
            }
        }

        private unsafe void Append(ulong value, bool signed, bool hex)
        {
            int offset = 0;

            ulong uvalue = (ulong)value;
            ushort divisor = hex ? (ushort)16 : (ushort)10;
            uint len = 0;
            uint count = 0;
            ulong temp;
            bool negative = false;

            if (value < 0 && !hex && signed)
            {
                count++;
                // TODO
                //uvalue = (ulong)-value;
                uvalue = (ulong)(-(uint)value);
                negative = true;
            }

            temp = uvalue;

            do
            {
                temp /= divisor;
                count++;
            }
            while (temp != 0);

            byte* first;
            fixed (byte* ptr = _chars)
            {
                first = ptr + this._length;
            }

            len = count;
            Length += len;

            if (negative)
            {
                *(first + offset) = (byte)'-';
                offset++;
                count--;
            }

            for (int i = 0; i < count; i++)
            {
                ulong remainder = uvalue % divisor;

                if (remainder < 10)
                    *(first + offset + count - 1 - i) = (byte)('0' + remainder);
                else
                    *(first + offset + count - 1 - i) = (byte)('A' + remainder - 10);

                uvalue /= divisor;
            }
        }

        public unsafe void Append(string format, uint arg0)
        {
            Append(format, arg0, 0, 0, 0);
        }

        public unsafe void Append(string format, string arg0)
        {
            Append(
                format,
                new Argument { String = arg0, Type = ArgumentType.String },
                new Argument(),
                new Argument(),
                new Argument(),
                new Argument(),
                new Argument(),
                new Argument());
        }

        public unsafe void Append(string format, string arg0, uint arg1)
        {
            Append(
                format,
                new Argument { String = arg0, Type = ArgumentType.String },
                new Argument { UInt = arg1, Type = ArgumentType.UInt },
                new Argument(),
                new Argument(),
                new Argument(),
                new Argument(),
                new Argument());
        }

        public unsafe void Append(string format, string arg0, int arg1)
        {
            Append(
                format,
                new Argument { String = arg0, Type = ArgumentType.String },
                new Argument { UInt = unchecked((uint)arg1), Type = ArgumentType.Int },
                new Argument(),
                new Argument(),
                new Argument(),
                new Argument(),
                new Argument());
        }

        public unsafe void Append(string format, uint arg0, uint arg1)
        {
            Append(format, arg0, arg1, 0);
        }

        public unsafe void Append(string format, int arg0, uint arg1)
        {
            Append(
                format,
                new Argument { UInt = (uint)arg0, Type = ArgumentType.Int },
                new Argument { UInt = arg1, Type = ArgumentType.UInt },
                new Argument(),
                new Argument(),
                new Argument(),
                new Argument(),
                new Argument());
        }

        public unsafe void Append(string format, uint arg0, uint arg1, uint arg2)
        {
            Append(
                format,
                new Argument { UInt = arg0, Type = ArgumentType.UInt },
                new Argument { UInt = arg1, Type = ArgumentType.UInt },
                new Argument { UInt = arg2, Type = ArgumentType.UInt },
                new Argument(),
                new Argument(),
                new Argument(),
                new Argument());
        }

        public unsafe void Append(string format, string arg0, uint arg1, uint arg2)
        {
            Append(
                format,
                new Argument { String = arg0, Type = ArgumentType.String },
                new Argument { UInt = arg1, Type = ArgumentType.UInt },
                new Argument { UInt = arg2, Type = ArgumentType.UInt },
                new Argument(),
                new Argument(),
                new Argument(),
                new Argument());
        }

        public unsafe void Append(string format, uint arg0, uint arg1, uint arg2, uint arg3)
        {
            Append(
                format,
                new Argument { UInt = arg0, Type = ArgumentType.UInt },
                new Argument { UInt = arg1, Type = ArgumentType.UInt },
                new Argument { UInt = arg2, Type = ArgumentType.UInt },
                new Argument { UInt = arg3, Type = ArgumentType.UInt },
                new Argument(),
                new Argument(),
                new Argument());
        }

        public unsafe void Append(string format, string arg0, uint arg1, uint arg2, uint arg3)
        {
            Append(
                format,
                new Argument { String = arg0, Type = ArgumentType.String },
                new Argument { UInt = arg1, Type = ArgumentType.UInt },
                new Argument { UInt = arg2, Type = ArgumentType.UInt },
                new Argument { UInt = arg3, Type = ArgumentType.UInt },
                new Argument(),
                new Argument(),
                new Argument());
        }

        public unsafe void Append(string format, uint arg0, uint arg1, uint arg2, uint arg3, uint arg4)
        {
            Append(
                format,
                new Argument { UInt = arg0, Type = ArgumentType.UInt },
                new Argument { UInt = arg1, Type = ArgumentType.UInt },
                new Argument { UInt = arg2, Type = ArgumentType.UInt },
                new Argument { UInt = arg3, Type = ArgumentType.UInt },
                new Argument { UInt = arg4, Type = ArgumentType.UInt },
                new Argument(),
                new Argument());
        }

        public unsafe void Append(string format, string arg0, uint arg1, uint arg2, uint arg3, uint arg4)
        {
            Append(
                format,
                new Argument { String = arg0, Type = ArgumentType.String },
                new Argument { UInt = arg1, Type = ArgumentType.UInt },
                new Argument { UInt = arg2, Type = ArgumentType.UInt },
                new Argument { UInt = arg3, Type = ArgumentType.UInt },
                new Argument { UInt = arg4, Type = ArgumentType.UInt },
                new Argument(),
                new Argument());
        }

        public unsafe void Append(string format, uint arg0, uint arg1, uint arg2, uint arg3, uint arg4, uint arg5)
        {
            Append(
                format,
                new Argument { UInt = arg0, Type = ArgumentType.UInt },
                new Argument { UInt = arg1, Type = ArgumentType.UInt },
                new Argument { UInt = arg2, Type = ArgumentType.UInt },
                new Argument { UInt = arg3, Type = ArgumentType.UInt },
                new Argument { UInt = arg4, Type = ArgumentType.UInt },
                new Argument { UInt = arg5, Type = ArgumentType.UInt },
                new Argument());
        }

        public unsafe void Append(string format, string arg0, uint arg1, uint arg2, uint arg3, uint arg4, uint arg5)
        {
            Append(
                format,
                new Argument { String = arg0, Type = ArgumentType.String },
                new Argument { UInt = arg1, Type = ArgumentType.UInt },
                new Argument { UInt = arg2, Type = ArgumentType.UInt },
                new Argument { UInt = arg3, Type = ArgumentType.UInt },
                new Argument { UInt = arg4, Type = ArgumentType.UInt },
                new Argument { UInt = arg5, Type = ArgumentType.UInt },
                new Argument());
        }

        public unsafe void Append(string format, uint arg0, uint arg1, uint arg2, uint arg3, uint arg4, uint arg5, uint arg6)
        {
            Append(
                format,
                new Argument { UInt = arg0, Type = ArgumentType.UInt },
                new Argument { UInt = arg1, Type = ArgumentType.UInt },
                new Argument { UInt = arg2, Type = ArgumentType.UInt },
                new Argument { UInt = arg3, Type = ArgumentType.UInt },
                new Argument { UInt = arg4, Type = ArgumentType.UInt },
                new Argument { UInt = arg5, Type = ArgumentType.UInt },
                new Argument { UInt = arg6, Type = ArgumentType.UInt });
        }

        private struct Argument
        {
            public uint UInt;
            public string String;
            public ArgumentType Type;
        }

        private enum ArgumentType
        {
            None = 0,
            UInt = 1,
            String = 2,
            Int = 3,
        }

        private unsafe void Append(string format, Argument arg0, Argument arg1, Argument arg2, Argument arg3, Argument arg4, Argument arg5, Argument arg6)
        {
            var indexBuffer = new StringBuffer
            {
                _length = 0,
            };
            var argsBuffer = new StringBuffer
            {
                _length = 0,
            };

            var inParam = false;
            var inArg = false;
            for (var i = 0; i < format.Length; i++)
            {

                if (format[i] == '{')
                {
                    inParam = true;
                    inArg = false;
                    continue;
                }

                if (format[i] == '}')
                {
                    switch (indexBuffer[0])
                    {
                        case '0':
                            Append(arg0, argsBuffer);
                            break;
                        case '1':
                            Append(arg1, argsBuffer);
                            break;
                        case '2':
                            Append(arg2, argsBuffer);
                            break;
                        case '3':
                            Append(arg3, argsBuffer);
                            break;
                        case '4':
                            Append(arg4, argsBuffer);
                            break;
                        case '5':
                            Append(arg5, argsBuffer);
                            break;
                        case '6':
                            Append(arg6, argsBuffer);
                            break;
                    }

                    inParam = false;
                    inArg = false;
                    indexBuffer.Clear();
                    argsBuffer.Clear();

                    continue;
                }

                if (inParam)
                {
                    if (format[i] == ':')
                    {
                        inArg = true;
                        continue;
                    }

                    if (inArg)
                        argsBuffer.Append(format[i]);
                    else
                        indexBuffer.Append(format[i]);
                    continue;
                }
                else
                {
                    Append(format[i]);
                    continue;
                }

            }
        }

        public void Append(uint val, byte digits)
        {
            Append(val, digits, -1);
        }

        public void Append(uint val, byte digits, int size)
        {
            uint count = 0;
            uint temp = val;

            do
            {
                temp /= digits;
                count++;
            }
            while (temp != 0);

            if (size != -1)
                count = (uint)size;
            var origPos = (uint)Length;
            Length += count;
            for (uint i = 0; i < count; i++)
            {
                uint digit = val % digits;
                uint charIdx = count - 1 - i;
                if (digit < 10)
                    this[origPos + charIdx] = (char)('0' + digit);
                else
                    this[origPos + charIdx] = (char)('A' + digit - 10);
                val /= digits;
            }
        }

        #endregion Append

        /// <summary>
        /// The length of the string
        /// </summary>
        /// <value>
        /// The length.
        /// </value>
        public uint Length
        {
            get
            {
                return _length;
            }

            set
            {
                if (value > MaxLength)
                {
                    //TODO: Error
                    value = MaxLength;
                }
                _length = value;
            }
        }

        /// <summary>
        /// Gets the index of a specific value
        /// </summary>
        public int IndexOf(string value)
        {
            if (this._length == 0)
                return -1;

            return IndexOfImpl(value, 0, this._length);
        }

        private int IndexOfImpl(string value, uint startIndex, uint count)
        {
            for (int i = (int)startIndex; i < count; i++)
            {
                bool found = true;
                for (int n = 0; n < value.Length; n++)
                {
                    if (this[i + n] != value[n])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                    return (int)i;
            }

            return -1;
        }

        public void WriteTo(StringBuffer sb)
        {
            sb.Append(this);
        }

        public unsafe void WriteTo(IBufferWriter handle)
        {
            fixed (byte* ptr = _chars)
            {
                for (var i = 0; i < _length; i++)
                {
                    handle.Write(ptr[i]);
                }
            }
        }

        public unsafe void WriteTo(Addr addr)
        {
            var ptr2 = (byte*)addr;

            fixed (byte* ptr = _chars)
            {
                for (var i = 0; i < _length; i++)
                {
                    ptr2[i] = (byte)ptr[i];
                }
            }
        }

        public unsafe string CreateString()
        {
            Append((char)0);
            fixed (byte* ptr = _chars)
            {
                var ptr2 = (sbyte*)ptr;
                return new string(ptr2);
            }

        }
    }
}
