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

    public static class ModularMath
    {

        /// <summary>
        /// Computes the greatest common divisor (GCD) of <paramref name="a"/> and <paramref name="b"/>,
        /// as well as the multiplicative inverse of <paramref name="a"/> modulo <paramref name="b"/>.
        /// </summary>
        /// <param name="a">A positive integer less than <paramref name="b"/>.</param>
        /// <param name="b">A positive integer greater than <paramref name="a"/>.</param>
        /// <param name="mulInverse">Receives the multiplicative inverse of <paramref name="a"/> modulo <paramref name="b"/>
        /// if <paramref name="a"/> and <paramref name="b"/> are coprime, or 0 otherwise.</param>
        /// <returns>The greatest common divisor of <paramref name="a"/> and <paramref name="b"/>.</returns>
        public static ulong ExtendedEuclidean(long a, long b, out ulong mulInverse)
        {
            if (a <= 0 || a >= b)
                throw new ArgumentOutOfRangeException("a must be a positive integer less than b");

            long r0 = b, r1 = a;  // ordered this way because b > a
            long q, r;
            long x0 = 0, x1 = 1;  // x is a's coefficient; we don't maintain b's coefficient
            long x = 1;  // return multiplicative inverse of 1 if loop breaks before x is assigned and if GCD (q) is 1

            for (; ; )
            {
                unchecked  // we know r0 and r1 are non-negative, even though they're signed types, so perform unsigned division for performance
                {
                    r = (long)((ulong)r0 % (ulong)r1);
                    if (r == 0) break;  // break before overwriting previous value of q

                    q = (long)((ulong)r0 / (ulong)r1);
                }

                x = (x0 - q * x1);

                r0 = r1; r1 = r;
                x0 = x1; x1 = x;
            }

            if (x < 0) x += b;
            mulInverse = (ulong)(r1 == 1 ? x : 0);

            return (ulong)r1;
        } //ModularMath.ExtendedEuclidean(long,long,out ulong)

        /// <summary>
        /// Computes the greatest common divisor (GCD) of <paramref name="a"/> and <paramref name="b"/>,
        /// as well as the multiplicative inverse of <paramref name="a"/> modulo <paramref name="b"/>.
        /// </summary>
        /// <param name="a">A positive integer less than <paramref name="b"/>.</param>
        /// <param name="b">A positive integer greater than <paramref name="a"/>.</param>
        /// <param name="mulInverse">Receives the multiplicative inverse of <paramref name="a"/> modulo <paramref name="b"/>
        /// if <paramref name="a"/> and <paramref name="b"/> are coprime, or null otherwise.</param>
        /// <returns>The greatest common divisor of <paramref name="a"/> and <paramref name="b"/>.</returns>
        public static MyUInt256 ExtendedEuclidean(MyUInt256 a, MyUInt256 b, out MyUInt256 mulInverse)
        {
            if (a.IsZero() || a.Compare(b) >= 0)
                throw new ArgumentOutOfRangeException("a must be a positive integer less than b");

            MyUInt256 r0 = new MyUInt256(b), r1 = new MyUInt256(a);  // ordered this way because b > a
            MyUInt256 q = new MyUInt256(), r = new MyUInt256();
            MyUInt256 x0 = new MyUInt256(0), x1 = new MyUInt256(1);  // x is a's coefficient; we don't maintain b's coefficient
            MyUInt256 x = new MyUInt256(1);  // return multiplicative inverse of 1 if loop breaks before x is assigned and if GCD (q) is 1
            MyUInt256 tmp = new MyUInt256();

            for (; ; )
            {
                r.Set(r0);  // r = r0 % r1
                r.Modulo(r1);
                if (r.IsZero()) break;  // break before overwriting previous value of q

                q.Set(r0);  // q = r0 / r1
                q.Divide(r1);

                x.Set(x0);  // x = x0 - q * x1
                tmp.Set(q);
                tmp.Multiply(x1);
                x.Subtract(tmp);

                r0.Set(r1); r1.Set(r);
                x0.Set(x1); x1.Set(x);
            }

            if (x.IsMsbSet()) x.Add(b);
            mulInverse = (r1.Compare(1) == 0 ? x : null);

            return r1;
        } //ModularMath.ExtendedEuclidean(MyUInt256,MyUInt256,out MyUInt256)

        /// <summary>
        /// Computes the multiplicative inverse of <paramref name="a"/> modulo <paramref name="modulus"/>.
        /// </summary>
        /// <param name="a">A non-negative integer less than <paramref name="modulus"/>.</param>
        /// <param name="modulus">A non-negative integer defining the integer ring in which <paramref name="a"/> resides.</param>
        /// <returns>The multiplicative inverse of <paramref name="a"/> modulo <paramref name="modulus"/>,
        /// or 0 if no such multiplicative inverse exists.</returns>
        public static ulong MultiplicativeInverse(long a, long modulus)
        {
            if (a < 0 || a >= modulus)
                throw new ArgumentOutOfRangeException("a must be a non-negative integer less than modulus");

            if (a == 0)
                return 0;

            ulong mi;
            ExtendedEuclidean(a, modulus, out mi);

            return mi;
        } //ModularMath.MultiplicativeInverse(long,long)

        /// <summary>
        /// Computes the multiplicative inverse of <paramref name="a"/> modulo <paramref name="modulus"/>.
        /// </summary>
        /// <param name="a">A non-negative integer less than <paramref name="modulus"/>.</param>
        /// <param name="modulus">A non-negative integer defining the integer ring in which <paramref name="a"/> resides.</param>
        /// <returns>The multiplicative inverse of <paramref name="a"/> modulo <paramref name="modulus"/>,
        /// or null if no such multiplicative inverse exists.</returns>
        public static MyUInt256 MultiplicativeInverse(MyUInt256 a, MyUInt256 modulus)
        {
            if (a.IsMsbSet() || a.Compare(modulus) >= 0)
                throw new ArgumentOutOfRangeException("a must be a non-negative integer less than modulus");

            if (a.IsZero())
                return null;

            MyUInt256 mi;
            ExtendedEuclidean(a, modulus, out mi);

            return mi;
        } //ModularMath.MultiplicativeInverse(MyUInt256,MyUInt256)

    } //class ModularMath

} //namespace Cylance.Research.Prangster
