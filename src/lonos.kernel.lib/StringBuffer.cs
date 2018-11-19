﻿/*
 * (c) 2014 MOSA - The Managed Operating System Alliance
 *
 * Licensed under the terms of the New BSD License.
 *
 * Authors:
 *  Sebastian Loncar (Arakis) <sebastian.loncar@gmail.com>
 */

using System.Runtime.InteropServices;

namespace lonos.kernel.core
{
    /// <summary>
    /// Represents a string as struct, so it can used before memory and runtime initialization.
    /// Use only where needed. Do not incease the struct size more as needed. A good limit would be the maximum horizontal text resolution.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct StringBuffer
    {
        private int length;

        public const int MaxLength = 132;
        public const int EntrySize = MaxLength * 2 + 4;

        unsafe private fixed char chars[MaxLength];

        unsafe public static StringBuffer CreateFromNullTerminatedString(uint start)
        {
            return CreateFromNullTerminatedString((byte*)start);
        }

        unsafe public static StringBuffer CreateFromNullTerminatedString(byte* start)
        {
            var buf = new StringBuffer();
            while (*start != 0)
            {
                buf.Append((char)*start++);
            }
            return buf;
        }

        /// <summary>
        /// Acces a char at a specific index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        unsafe public char this[int index]
        {
            get
            {
                if (index < 0 || index >= Length) //TODO: Error
                    return '\x0';

                fixed (char* ptr = chars)
                    return ptr[index];
            }
            set
            {
                if (index < 0 || index >= Length) //TODO: Error
                    return;
                fixed (char* ptr = chars)
                    ptr[index] = value;
            }
        }

        unsafe public char this[uint index]
        {
            get
            {
                if (index >= Length) //TODO: Error
                    return '\x0';
                fixed (char* ptr = chars)
                    return ptr[index];
            }
            set
            {
                if (index >= Length) //TODO: Error
                    return;
                fixed (char* ptr = chars)
                    ptr[index] = value;
            }
        }

        public void Clear()
        {
            length = 0;
        }

        /// <summary>
        /// Overwrite the current value with a new one
        /// </summary>
        /// <param name="value"></param>
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

        #endregion Constructor

        #region Append

        /// <summary>
        /// Appends a string
        /// </summary>
        /// <param name="value"></param>
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
        /// <param name="value"></param>
        public unsafe void Append(NullTerminatedString* value)
        {
            var len = value->GetLength();
            for (var i = 0; i < len; i++)
                Append(value->Bytes[i]);
        }

        public void Append(string value, int start)
        {
            if (value == null) return;
            Append(value, start, value.Length - start);
        }

        public void Append(string value, int start, int length)
        {
            if (value == null) return;
            for (var i = 0; i < length; i++)
                Append(value[i + start]);
        }

        public void Append(StringBuffer value)
        {
            if (value.length == 0)
                return;

            for (var i = 0; i < value.Length; i++)
                Append(value[i]);
        }

        public void Append(StringBuffer value, int start)
        {
            if (value.length == 0) return;
            Append(value, start, value.Length - start);
        }

        public void Append(StringBuffer value, int start, int length)
        {
            if (value.length == 0) return;
            for (var i = 0; i < length; i++)
                Append(value[i + start]);
        }

        unsafe public void Append(char value)
        {
            if (length + 1 >= MaxLength)
            {
                //TODO: Error
                return;
            }
            //isSet = 1;
            length++;
            this[length - 1] = value;
        }

        public void Append(uint value)
        {
            Append(value, false, false);
        }

        public void Append(int value)
        {
            Append((uint)value, true, false);
        }

        /// <summary>
        /// Appends a number to the string. Use format to output as Hex.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="format"></param>
        public void Append(uint value, string format)
        {
            Append(value, false, format.Length == 1 && format[0] == 'X');
        }

        public void Append(uint value, StringBuffer format)
        {
            Append(value, false, format.Length == 1 && format[0] == 'X');
        }

        /// <summary>
        /// Appends a number to the string. Use format to output as Hex.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="format"></param>
        public void Append(int value, string format)
        {
            var u = (uint)value;
            Append(u, true, true);
        }

        unsafe private void Append(uint value, bool signed, bool hex)
        {
            int offset = 0;

            uint uvalue = (uint)value;
            ushort divisor = hex ? (ushort)16 : (ushort)10;
            int len = 0;
            int count = 0;
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

            char* first;
            fixed (char* ptr = chars)
            {
                first = (ptr + this.length);
            }

            len = count;
            Length += len;

            if (negative)
            {
                *(first + offset) = '-';
                offset++;
                count--;
            }

            for (int i = 0; i < count; i++)
            {
                uint remainder = uvalue % divisor;

                if (remainder < 10)
                    *(first + offset + count - 1 - i) = (char)('0' + remainder);
                else
                    *(first + offset + count - 1 - i) = (char)('A' + remainder - 10);

                uvalue /= divisor;
            }
        }

        public unsafe void Append(string format, uint arg0)
        {
            Append(format, arg0, 0, 0);
        }

        public unsafe void Append(string format, uint arg0, uint arg1)
        {
            Append(format, arg0, arg1, 0);
        }

        public unsafe void Append(string format, uint arg0, uint arg1, uint arg2)
        {
            var indexBuffer = new StringBuffer();
            indexBuffer.length = 0;
            var argsBuffer = new StringBuffer();
            argsBuffer.length = 0;

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
                    inParam = false;
                    inArg = false;

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
                    }

                    inParam = false;
                    inArg = false;

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
            } while (temp != 0);

            if (size != -1)
                count = (uint)size;


            uint charIdx = 0;
            var origPos = (uint)Length;
            Length += (int)count;
            for (uint i = 0; i < count; i++)
            {
                uint digit = val % digits;
                charIdx = count - 1 - i;
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
        public int Length
        {
            get { return length; }
            set
            {
                if (value > MaxLength)
                {
                    //TODO: Error
                    value = MaxLength;
                }
                length = value;
                //isSet = 1;
            }
        }

        /// <summary>
        /// Gets the index of a specific value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int IndexOf(string value)
        {
            if (this.length == 0)
                return -1;

            return IndexOfImpl(value, 0, this.length);
        }

        private int IndexOfImpl(string value, int startIndex, int count)
        {
            for (int i = startIndex; i < count; i++)
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
                    return i;
            }

            return -1;
        }
    }
}