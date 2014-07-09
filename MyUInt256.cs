/*
 Copyright (c) 2013 Cylance, Inc.  All rights reserved.
 For updates, please visit <http://www.cylance.com/>.

 Redistribution and use in source and binary forms, with or without
 modification, are permitted provided that the following conditions are met:
 1. Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer.
 2. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.
 3. All advertising materials mentioning features or use of this software
    must display the following acknowledgement:
    This product includes software developed by Cylance, Inc.
 4. Neither the name of Cylance, Inc., nor the names of its contributors
    may be used to endorse or promote products derived from this software
    without specific prior written permission.

 THIS SOFTWARE IS PROVIDED BY CYLANCE, INC., ''AS IS'' AND ANY EXPRESS OR
 IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO
 EVENT SHALL CYLANCE, INC., BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
 OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;

namespace Cylance.Research.Prangster
{

    public sealed class MyUInt256
    {

        private const int SizeInLongs = 4;

        private readonly ulong[] _A;

        public MyUInt256()
        {
            this._A = new ulong[SizeInLongs];
        }

        public MyUInt256(ulong value)
            : this()
        {
            this._A[0] = value;
        }

        public MyUInt256(ulong low, ulong high)
            : this()
        {
            this._A[0] = low;
            this._A[1] = high;
        }

        public MyUInt256(MyUInt256 value)
        {
            this._A = (ulong[])value._A.Clone();
        }

        private static int CompareInternal(ulong[] a, ulong[] b)
        {
            for (int i = SizeInLongs - 1; i >= 0; i--)
            {
                if (a[i] < b[i])
                    return -1;
                else if (a[i] > b[i])
                    return 1;
            }

            return 0;
        }

        private static int CompareInternal(uint[] a, uint[] b)
        {
            for (int i = (SizeInLongs * 2) - 1; i >= 0; i--)
            {
                if (a[i] < b[i])
                    return -1;
                else if (a[i] > b[i])
                    return 1;
            }

            return 0;
        }

        public static int Compare(MyUInt256 a, MyUInt256 b)
        {
            return CompareInternal(a._A, b._A);
        }

        public int Compare(MyUInt256 value)
        {
            return Compare(this, value);
        }

        public int Compare(ulong value)
        {
            for (int i = SizeInLongs - 1; i > 0; i--)
            {
                if (this._A[i] != 0)
                    return 1;
            }

            return this._A[0].CompareTo(value);
        }

        public static bool IsZero(MyUInt256 value)
        {
            for (int i = 0; i < SizeInLongs; i++)
            {
                if (value._A[i] != 0)
                    return false;
            }

            return true;
        }

        public bool IsZero()
        {
            return IsZero(this);
        }

        public bool IsMsbSet()
        {
            return (this._A[SizeInLongs - 1] & (1UL << 63)) != 0;
        }

        public static explicit operator MyUInt256(ulong value)
        {
            return new MyUInt256(value);
        }

        public static MyUInt256 operator +(MyUInt256 a, MyUInt256 b)
        {
            MyUInt256 sum = new MyUInt256(a);
            sum.Add(b);
            return sum;
        }

        public ulong ToUInt64()
        {
            return this._A[0];
        }

        public void Zero()
        {
            for (int i = 0; i < SizeInLongs; i++)
                this._A[i] = 0;
        }

        public void Set(ulong value)
        {
            this._A[0] = value;

            for (int i = 1; i < SizeInLongs; i++)
                this._A[i] = 0;
        }

        public void Set(ulong low, ulong high)
        {
            this._A[0] = low;
            this._A[1] = high;

            for (int i = 2; i < SizeInLongs; i++)
                this._A[i] = 0;
        }

        public void Set(MyUInt256 value)
        {
            for (int i = 0; i < SizeInLongs; i++)
                this._A[i] = value._A[i];
        }

        public void Add(ulong value)
        {
            unchecked
            {
                for (int i = 0; i < SizeInLongs && value != 0; i++)
                {
                    ulong oldvalue = value;
                    value = 0;

                    if ((this._A[i] += oldvalue) < oldvalue)
                        value++;
                }
            }
        }

        public void Add(MyUInt256 value)
        {
            unchecked
            {
                ulong carry = 0;

                for (int i = 0; i < SizeInLongs; i++)
                {
                    ulong oldcarry = carry;
                    carry = 0;

                    if ((this._A[i] += value._A[i]) < value._A[i])
                        carry++;
                    if ((this._A[i] += oldcarry) < oldcarry)
                        carry++;
                }
            }
        }

        public static MyUInt256 operator -(MyUInt256 a, MyUInt256 b)
        {
            MyUInt256 diff = new MyUInt256(a);
            diff.Subtract(b);
            return diff;
        }

        public void Subtract(ulong value)
        {
            unchecked
            {
                for (int i = 0; i < SizeInLongs; i++)
                {
                    ulong oldvalue = value;
                    value = 0;

                    if ((this._A[i] -= oldvalue) > ~oldvalue)
                        value++;
                    else break;
                }
            }
        }

        private static void SubtractInternal(ulong[] minuend, ulong[] subtrahend)
        {
            unchecked
            {
                ulong borrow = 0;

                for (int i = 0; i < SizeInLongs; i++)
                {
                    ulong oldborrow = borrow;
                    borrow = 0;

                    if ((minuend[i] -= subtrahend[i]) > ~subtrahend[i])
                        borrow++;
                    if ((minuend[i] -= oldborrow) > ~oldborrow)
                        borrow++;
                }
            }
        }

        private static void SubtractInternal(uint[] minuend, uint[] subtrahend)
        {
            unchecked
            {
                uint borrow = 0;

                for (int i = 0; i < (SizeInLongs * 2); i++)
                {
                    uint oldborrow = borrow;
                    borrow = 0;

                    if ((minuend[i] -= subtrahend[i]) > ~subtrahend[i])
                        borrow++;
                    if ((minuend[i] -= oldborrow) > ~oldborrow)
                        borrow++;
                }
            }
        }

        public void Subtract(MyUInt256 value)
        {
            SubtractInternal(this._A, value._A);
        }

        private static uint[] GetHalfSizeArrayInternal(ulong[] value)
        {
            uint[] array = new uint[SizeInLongs * 2];

            unchecked
            {
                for (int j = 0, i = 0; i < SizeInLongs; i++)
                {
                    ulong qw = value[i];
                    array[j++] = (uint)qw;
                    array[j++] = (uint)(qw >> 32);
                }
            }

            return array;
        }

        private uint[] GetHalfSizeArray()
        {
            return GetHalfSizeArrayInternal(this._A);
        }

        private void StoreHalfSizeArray(uint[] array)
        {
            for (int j = 0, i = 0; i < SizeInLongs; j += 2, i++)
            {
                this._A[i] = (ulong)array[j] | ((ulong)array[j + 1] << 32);
            }
        }

        private static uint[] MultiplyInternal(uint[] a, uint[] b)
        {
            //     A7   A6   A5   A4   A3   A2   A1   A0
            // *   B7   B6   B5   B4   B3   B2   B1   B0
            // =========================================
            //   B0A7 B0A6 B0A5 B0A4 B0A3 B0A2 B0A1 B0A0
            //   B1A6 B1A5 B1A4 B1A3 B1A2 B1A1 B1A0
            //   B2A5 B2A4 B2A3 B2A2 B2A1 B2A0
            //   B3A4 B3A3 B3A2 B3A1 B3A0
            //   B4A3 B4A2 B4A1 B4A0
            //   B5A2 B5A1 B5A0
            //   B6A1 B6A0
            // + B7A0

            uint[] product = new uint[SizeInLongs * 2];

            for (int j = 0; j < (SizeInLongs * 2); j++)
            {
                ulong qw = 0;
                for (int i = 0; i < (SizeInLongs * 2) - j; i++)
                {
                    unchecked
                    {
                        qw += (ulong)a[i] * (ulong)b[j];
                        qw += (ulong)product[j + i];
                        product[j + i] = (uint)qw;
                        qw >>= 32;
                    }
                }
            }

            return product;
        } //MyUInt256.MultiplyInternal

        private void MultiplyInternal(uint[] b)
        {
            uint[] a = this.GetHalfSizeArray();

            uint[] product = MultiplyInternal(a, b);

            this.StoreHalfSizeArray(product);
        }

        public void Multiply(ulong value)
        {
            uint[] multiplicand = new uint[SizeInLongs * 2];

            unchecked
            {
                multiplicand[0] = (uint)value;
                multiplicand[1] = (uint)(value >> 32);
            }

            this.MultiplyInternal(multiplicand);
        }

        public void Multiply(MyUInt256 value)
        {
            this.MultiplyInternal(value.GetHalfSizeArray());
        }

        public void Square()
        {
            uint[] value = this.GetHalfSizeArray();

            this.StoreHalfSizeArray(
                MultiplyInternal(value, value));
        }

        private void ExpModInternal(ulong[] exponent, ulong[] divisor)
        {
            int exponentmsb = Msb(exponent);
            if (exponentmsb < 0)
            {
                this.Set(1);
                return;
            }

            uint[] divhsa;
            if (divisor != null)
                divhsa = GetHalfSizeArrayInternal(divisor);
            else divhsa = null;

            uint[] power = new uint[SizeInLongs * 2];
            power[0] = 1;

            uint[] square = this.GetHalfSizeArray();

            ulong bit = 1;
            for (int i = 0; ; )
            {
                if ((exponent[i] & bit) != 0)
                {
                    power = MultiplyInternal(power, square);

                    if (divhsa != null)
                        ModuloInternal(power, divhsa);
                }

                if (--exponentmsb < 0)
                    break;

                square = MultiplyInternal(square, square);

                if (divhsa != null)
                    ModuloInternal(square, divhsa);

                if ((bit <<= 1) == 0)
                {
                    bit = 1;
                    i++;
                }
            }

            this.StoreHalfSizeArray(power);
        } //MyUInt256.ExpModInternal

        public void Exponentiate(ulong exponent)
        {
            ulong[] exparray = new ulong[SizeInLongs];
            exparray[0] = exponent;

            ExpModInternal(exparray, null);
        }

        public void ExpMod(ulong exponent, MyUInt256 divisor)
        {
            ulong[] exparray = new ulong[SizeInLongs];
            exparray[0] = exponent;

            ExpModInternal(exparray, divisor._A);
        }

        private static int Msb(uint value)
        {
            if (value == 0)
                return -1;

            int msb = 0;

            unchecked
            {
                for (int i = 16; i != 0; i >>= 1)
                {
                    if ((value >> i) != 0)
                    {
                        value = (value >> i);
                        msb += i;
                    }
                }
            } //unchecked

            return msb;
        } //MyUInt256.Msb(uint)

        private static int Msb(ulong value)
        {
            if (value == 0)
                return -1;

            int msb;

            unchecked
            {
                uint dw;

                if ((value >> 32) != 0)
                {
                    dw = (uint)(value >> 32);
                    msb = 32;
                }
                else
                {
                    dw = (uint)value;
                    msb = 0;
                }

                return msb + Msb(dw);
            } //unchecked
        } //MyUInt256.Msb(ulong)

        private static int Msb(ulong[] array)
        {
            for (int i = SizeInLongs - 1; i >= 0; i--)
            {
                int msb = Msb(array[i]);
                if (msb >= 0)
                    return (i * 64) + msb;
            }

            return -1;
        } //MyUInt256.Msb(ulong[])

        private static int Msb(uint[] array)
        {
            for (int i = (SizeInLongs * 2) - 1; i >= 0; i--)
            {
                int msb = Msb(array[i]);
                if (msb >= 0)
                    return (i * 32) + msb;
            }

            return -1;
        } //MyUInt256.Msb(uint[])

        private static void ShiftLeftInternal(ulong[] array, int count)
        {
            if (count >= 64)
            {
                int longcount = (count / 64);
                count %= 64;

                int i;
                for (i = SizeInLongs - 1; i >= longcount; i--)
                    array[i] = array[i - longcount];
                for (; i >= 0; i--)
                    array[i] = 0;
            }

            if (count <= 0)
                return;

            ulong carryin = 0;
            for (int i = 0; i < SizeInLongs; i++)
            {
                ulong carryout = (array[i] >> (64 - count));
                array[i] = (array[i] << count) | carryin;
                carryin = carryout;
            }
        } //MyUInt256.ShiftLeftInternal(ulong[],int)

        private static void ShiftLeftInternal(uint[] array, int count)
        {
            if (count >= 32)
            {
                int intcount = (count / 32);
                count %= 32;

                int i;
                for (i = (SizeInLongs * 2) - 1; i >= intcount; i--)
                    array[i] = array[i - intcount];
                for (; i >= 0; i--)
                    array[i] = 0;
            }

            if (count <= 0)
                return;

            uint carryin = 0;
            for (int i = 0; i < (SizeInLongs * 2); i++)
            {
                uint carryout = (array[i] >> (32 - count));
                array[i] = (array[i] << count) | carryin;
                carryin = carryout;
            }
        } //MyUInt256.ShiftLeftInternal(uint[],int)

        private static void ShiftRightInternal(ulong[] array, int count)
        {
            if (count >= 64)
            {
                int longcount = (count / 64);
                count %= 64;

                int i;
                for (i = 0; i < (SizeInLongs - longcount); i++)
                    array[i] = array[i + longcount];
                for (; i < SizeInLongs; i++)
                    array[i] = 0;
            }

            if (count <= 0)
                return;

            ulong carryin = 0;
            for (int i = SizeInLongs - 1; i >= 0; i--)
            {
                ulong carryout = (array[i] << (64 - count));
                array[i] = (array[i] >> count) | carryin;
                carryin = carryout;
            }
        } //MyUInt256.ShiftRightInternal(ulong[],int)

        private static void ShiftRightInternal(uint[] array, int count)
        {
            if (count >= 32)
            {
                int intcount = (count / 32);
                count %= 32;

                int i;
                for (i = 0; i < ((SizeInLongs * 2) - intcount); i++)
                    array[i] = array[i + intcount];
                for (; i < (SizeInLongs * 2); i++)
                    array[i] = 0;
            }

            if (count <= 0)
                return;

            uint carryin = 0;
            for (int i = (SizeInLongs * 2) - 1; i >= 0; i--)
            {
                uint carryout = (array[i] << (32 - count));
                array[i] = (array[i] >> count) | carryin;
                carryin = carryout;
            }
        } //MyUInt256.ShiftRightInternal(uint[],int)

        private void ModuloInternal(ulong[] divisor)
        {
            int divisorshift;

            {
                int dividendmsb = Msb(this._A);
                int divisormsb = Msb(divisor);

                if (dividendmsb > divisormsb)
                {
                    divisorshift = (dividendmsb - divisormsb);
                    ShiftLeftInternal(divisor, divisorshift);
                }
                else divisorshift = 0;
            }

            for (; ; )
            {
                if (CompareInternal(this._A, divisor) >= 0)
                    SubtractInternal(this._A, divisor);

                if (divisorshift-- == 0)
                    break;

                ShiftRightInternal(divisor, 1);
            }
        } //MyUInt256.ModuloInternal

        private static void ModuloInternal(uint[] dividend, uint[] divisor)
        {
            int divisorshift;

            {
                int dividendmsb = Msb(dividend);
                int divisormsb = Msb(divisor);

                if (dividendmsb > divisormsb)
                {
                    divisorshift = (dividendmsb - divisormsb);
                    ShiftLeftInternal(divisor, divisorshift);
                }
                else divisorshift = 0;
            }

            for (; ; )
            {
                if (CompareInternal(dividend, divisor) >= 0)
                    SubtractInternal(dividend, divisor);

                if (divisorshift-- == 0)
                    break;

                ShiftRightInternal(divisor, 1);
            }
        } //MyUInt256.ModuloInternal

        public void Modulo(ulong divisor)
        {
            if (divisor == 0)
                return;

            unchecked
            {
                this._A[SizeInLongs - 1] %= divisor;
            }

            ulong[] array = new ulong[SizeInLongs];
            array[0] = divisor;

            this.ModuloInternal(array);
        }

        public void Modulo(MyUInt256 divisor)
        {
            int i;
            for (i = 0; i < SizeInLongs; i++)
                if (divisor._A[i] != 0) break;

            if (i == SizeInLongs)
                return;

            this.ModuloInternal((ulong[])divisor._A.Clone());
        }

        private void DivideInternal(ulong[] divisor)
        {
            ulong[] dividend = (ulong[])this._A.Clone();
            this.Zero();

            int divisorshift;

            {
                int dividendmsb = Msb(dividend);
                int divisormsb = Msb(divisor);

                if (dividendmsb > divisormsb)
                {
                    divisorshift = (dividendmsb - divisormsb);
                    ShiftLeftInternal(divisor, divisorshift);
                }
                else if (dividendmsb < divisormsb)
                    return;
                else divisorshift = 0;
            }

            for (; ; )
            {
                if (CompareInternal(dividend, divisor) >= 0)
                {
                    SubtractInternal(dividend, divisor);

                    unchecked
                    {
                        this._A[divisorshift / 64] |= (ulong)(1UL << (divisorshift % 64));
                    }
                }

                if (divisorshift-- == 0)
                    break;

                ShiftRightInternal(divisor, 1);
            }
        } //MyUInt256.DivideInternal

        public void Divide(ulong divisor)
        {
            if (divisor == 0)
            {
                ShiftRightInternal(this._A, 64);
                return;
            }

            int divisormsb = Msb(divisor);

            if ((divisor & (divisor - 1)) == 0)
            {
                ShiftRightInternal(this._A, divisormsb);
                return;
            }
            else if (divisormsb < 32)
            {
                uint[] a = this.GetHalfSizeArray();

                ulong remainder = 0;
                for (int i = (SizeInLongs * 2) - 1; i >= 0; i--)
                {
                    unchecked
                    {
                        ulong qw = (remainder | a[i]);
                        remainder = (ulong)(qw % divisor) << 32;
                        a[i] = (uint)(qw / divisor);
                    }
                }

                this.StoreHalfSizeArray(a);
                return;
            }

            ulong[] array = new ulong[SizeInLongs];
            array[0] = divisor;

            this.DivideInternal(array);
        } //MyUInt256.Divide(ulong)

        public void Divide(MyUInt256 divisor)
        {
            int i;
            for (i = 0; i < SizeInLongs; i++)
                if (divisor._A[i] != 0) break;

            if (i == SizeInLongs)
                throw new DivideByZeroException("divisor must be non-zero");

            this.DivideInternal((ulong[])divisor._A.Clone());
        }

    } //struct MyUInt256

} //namespace Cylance.Research.Prangster
