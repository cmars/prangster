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

// Based on Microsoft SSCLI/Rotor random.cs (modified 06/14/2013), which bears the following copyright notice:
//   Copyright (c) Microsoft Corporation. All rights reserved.
//
// and is subject to the following license:
/*
MICROSOFT SHARED SOURCE CLI, C#, AND JSCRIPT LICENSE

This License governs use of the accompanying Software, and your use of
the Software constitutes acceptance of this license.

You may use this Software for any non-commercial purpose, subject to
the restrictions in this license. Some purposes which can be
non-commercial are teaching, academic research, and personal
experimentation.   You may also distribute this Software with books or
other teaching materials, or publish the Software on websites, that
are intended to teach the use of the Software.

You may not use or distribute this Software or any derivative works in
any form for commercial purposes.  Examples of commercial purposes
would be running business operations, licensing, leasing, or selling
the Software, or distributing the Software for use with commercial
products.

You may modify this Software and distribute the modified Software for
non-commercial purposes, however, you may not grant rights to the
Software or derivative works that are broader than those provided by
this License.   For example, you may not distribute modifications of
the Software under terms that would permit commercial use, or under
terms that purport to require the Software or derivative works to be
sublicensed to others.

You may use any information in intangible form that you remember after
accessing the Software.  However, this right does not grant you a
license to any of Microsoft's copyrights or patents for anything you
might create using such information.

In return, we simply require that you agree:

1.  Not to remove any copyright or other notices from the Software.

2.  That if you distribute the Software in source or object form,
    you will include a verbatim copy of this license.

3.  That if you distribute derivative works of the Software in
    source code form you do so only under a license that
    includes all of the provisions of this License, and if you
    distribute derivative works of the Software solely in object
    form you do so only under a license that complies with this
    License.

4.  That if you have modified the Software or created derivative
    works, and distribute such modifications or derivative
    works, you will cause the modified files to carry prominent
    notices so that recipients know that they are not receiving
    the original Software.  Such notices must state: (i) that
    you have changed the Software; and (ii) the date of any
    changes.

5.  THAT THE SOFTWARE COMES "AS IS", WITH NO WARRANTIES.  THIS
    MEANS NO EXPRESS, IMPLIED OR STATUTORY WARRANTY, INCLUDING
    WITHOUT LIMITATION, WARRANTIES OF MERCHANTABILITY OR FITNESS
    FOR A PARTICULAR PURPOSE OR ANY WARRANTY OF TITLE OR
    NON-INFRINGEMENT.  ALSO, YOU MUST PASS THIS DISCLAIMER ON
    WHENEVER YOU DISTRIBUTE THE SOFTWARE OR DERIVATIVE WORKS.

6.  THAT MICROSOFT WILL NOT BE LIABLE FOR ANY DAMAGES RELATED TO
    THE SOFTWARE OR THIS LICENSE, INCLUDING DIRECT, INDIRECT,
    SPECIAL, CONSEQUENTIAL OR INCIDENTAL DAMAGES, TO THE MAXIMUM
    EXTENT THE LAW PERMITS, NO MATTER WHAT LEGAL THEORY IT IS
    BASED ON.  ALSO, YOU MUST PASS THIS LIMITATION OF LIABILITY
    ON WHENEVER YOU DISTRIBUTE THE SOFTWARE OR DERIVATIVE
    WORKS.

7.  That if you sue anyone over patents that you think may apply
    to the Software or anyone's use of the Software, your
    license to the Software ends automatically.

8.  That your rights under the License end automatically if you
    breach it in any way.

9.  Microsoft reserves all rights not expressly granted to you in
    this license.
*/

using System;

namespace Cylance.Research.Prangster
{

    public abstract class PrngDotNetBase : PrngBase
    {

        protected const int MBIG = 2147483647;
        protected const int MSEED = 161803398;

        private int inext, inextp;
        private readonly int[] SeedArray = new int[56];

        private readonly uint[,] _NextMatrix;
        private readonly uint[,] _PreviousMatrix;

        public PrngDotNetBase()
        {
            this._NextMatrix = GenerateNextMatrix();
            this._PreviousMatrix = GeneratePreviousMatrix();
        }

        public PrngDotNetBase(ulong seed)
            : this()
        {
            this.Seed(seed);
        }

        public override ulong MinimumSeed
        {
            get
            {
                return 0;
            }
        }

        public override ulong MaximumSeed
        {
            get
            {
                return int.MaxValue;
            }
        }

        public override void Seed(ulong seed)
        {
            //if (seed > int.MaxValue)
            //    throw new ArgumentOutOfRangeException("seed must be a non-negative 32-bit integer");

            unchecked  // disable arithmetic checks for performance
            {
                int Seed = (int)(seed & 0x7FFFFFFF);

                int ii;
                int mj, mk;
    
                //Initialize our Seed array.
                //This algorithm comes from Numerical Recipes in C (2nd Ed.)
                mj = MSEED - Seed;
                SeedArray[55] = mj;
                mk = 1;
                for (int i = 1; i < 55; i++)  //Apparently the range [1..55] is special (Knuth) and so we're wasting the 0'th position.
                {
                    ii = (21 * i) % 55;
                    SeedArray[ii] = mk;
                    mk = mj - mk;
                    if (mk < 0) mk += MBIG;
                    mj = SeedArray[ii];
                }

                for (int k = 1; k < 5; k++)
                {
                    for (int i = 1; i < 56; i++)
                    {
                        SeedArray[i] -= SeedArray[1 + (i + 30) % 55];
                        if (SeedArray[i] < 0)
                            SeedArray[i] += MBIG;
                    }
                }
            } //unchecked

            inext = 0;
            inextp = 21;
        } //PrngDotNetBase.Seed

        public override ulong Next()  // Random.Next()
        {
            int retVal;
            int locINext = inext;
            int locINextp = inextp;

            unchecked  // disable arithmetic checks for performance
            {
                if (++locINext >= 56) locINext = 1;
                if (++locINextp >= 56) locINextp = 1;

                retVal = SeedArray[locINext] - SeedArray[locINextp];

                if (retVal < 0) retVal += MBIG;

                SeedArray[locINext] = retVal;

                inext = locINext;
                inextp = locINextp;

                return (ulong)(long)retVal;
            }
        } //PrngDotNetBase.Next()

        public override bool CanReverse
        {
            get
            {
                return true;
            }
        }

        public override ulong Previous()
        {
            unchecked  // disable arithmetic checks for performance
            {
                if (this.inext == 0)
                    this.inext = 55;

                int current = this.SeedArray[this.inext];

                this.SeedArray[this.inext] += this.SeedArray[this.inextp];
                if ((uint)this.SeedArray[this.inext] >= MBIG)
                    this.SeedArray[this.inext] -= MBIG;

                if (--this.inext == 0) this.inext = 55;
                if (--this.inextp == 0) this.inextp = 55;

                return (ulong)(long)current;
            }
        } //PrngDotNetBase.Previous

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        public override void SeekAhead(ulong offset)
        {
            if (offset < 55)
            {
                Step(unchecked((int)offset));
                return;
            }

            int steps;
            if (this.inext != 0 && this.inext != 55)
            {
                steps = (55 - this.inext);
                Step(steps);

                offset -= unchecked((uint)steps);
            }

            ulong blockoffset = (offset / 55);

            steps = unchecked((int)(offset % 55));
            if (steps >= 28)
            {
                blockoffset++;  // advance one more block and then go back to reduce the number of steps (although this could be slower due to more matrix multiplications)
                steps -= 55;
            }

            if (blockoffset != 0)
            {
                BlockSeek(blockoffset, this._NextMatrix);
            }

            Step(steps);
        } //PrngDotNetBase.SeekAhead

        public override void SeekBack(ulong offset)
        {
            if (offset < 55)
            {
                Step(unchecked(-(int)offset));
                return;
            }

            int steps;
            if (this.inext != 0 && this.inext != 55)
            {
                steps = this.inext;
                Step(-steps);

                offset -= unchecked((uint)steps);
            }

            ulong blockoffset = (offset / 55);

            steps = unchecked(-(int)(offset % 55));
            if (steps <= -28)
            {
                blockoffset++;  // go back one more block and then advance to reduce the number of steps (although this could be slower due to more matrix multiplications)
                steps += 55;
            }

            if (blockoffset != 0)
            {
                BlockSeek(blockoffset, this._PreviousMatrix);
            }

            Step(steps);
        } //PrngDotNetBase.SeekBack

        protected void Step(int offset)
        {
            if (offset < 0)
            {
                for (; offset != 0; offset++)
                    Previous();
            }
            else
            {
                for (; offset != 0; offset--)
                    Next();
            }
        } //PrngDotNetBase.Step

        protected void BlockSeek(ulong blockOffset, uint[,] matrix)
        {
            // compute 'blockOffset'-th power of 'matrix'
            uint[,] matrixa = (uint[,])(matrix.Clone());
            uint[,] matrixb = new uint[55, 55];

            uint[,] seekmatrixa = new uint[55, 55];
            uint[,] seekmatrixb = new uint[55, 55];
            for (int i = 0; i < 55; i++)  // initialize seek matrix to identity matrix
                seekmatrixa[i, i] = 1;

            bool desta = false;  // allocate two matrices and switch between them, instead of constantly allocating new matrices
            bool seekdesta = false;

            for (; ; )
            {
                if ((blockOffset & 1) != 0)
                {
                    MultiplyMatrix((seekdesta ? seekmatrixa : seekmatrixb), (seekdesta ? seekmatrixb : seekmatrixa), (desta ? matrixb : matrixa));
                    seekdesta = !seekdesta;
                }

                if ((blockOffset >>= 1) == 0)
                    break;

                MultiplyMatrix((desta ? matrixa : matrixb), (desta ? matrixb : matrixa), (desta ? matrixb : matrixa));  // compute square of matrix
                desta = !desta;
            }

            // apply seek matrix to current state vector
            if (seekdesta)
                seekmatrixa = seekmatrixb;  // if 'seekmatrixa' is set to be the next destination, then 'seekmatrixb' must contain the product last computed

            int[] newstate = new int[55];

            for (int i = 0; i < 55; i++)
            {
                uint accumulator = 0;

                for (int k = 0; k < 55; k++)
                {
                    accumulator += unchecked((uint)(((ulong)seekmatrixa[i, k] * (ulong)this.SeedArray[1 + k]) % MBIG));
                    if (accumulator >= MBIG) accumulator -= MBIG;
                }

                newstate[i] = (int)accumulator;
            }

            Array.Copy(newstate, 0, this.SeedArray, 1, 55);
        } //PrngDotNetBase.BlockSeek

        private static uint[,] GenerateNextMatrix()
        {
            uint[,] matrix = new uint[55, 55];

            for (int i = 0; i < 34; i++)
            {
                matrix[i, i] = 1;
                matrix[i, 21 + i] = (MBIG - 1);
            }

            for (int i = 0; i < 21; i++)
            {
                matrix[34 + i, i] = (MBIG - 1);
                matrix[34 + i, 21 + i] = 1;
                matrix[34 + i, 34 + i] = 1;
            }

            return matrix;
        } //PrngDotNetBase.GenerateNextMatrix

        private static uint[,] GeneratePreviousMatrix()
        {
/* inverse matrix:
1000000010000000000001000000000000000000001000000000000
0100000001000000000000100000000000000000000100000000000
0010000000100000000000010000000000000000000010000000000
0001000000010000000000001000000000000000000001000000000
0000100000001000000000000100000000000000000000100000000
0000010000000100000000000010000000000000000000010000000
0000001000000010000000000001000000000000000000001000000
0000000100000001000000000000100000000000000000000100000
0000000010000000100000000000010000000000000000000010000
0000000001000000010000000000001000000000000000000001000
0000000000100000001000000000000100000000000000000000100
0000000000010000000100000000000010000000000000000000010
0000000000001000000010000000000001000000000000000000001
1000000000000100000000000000000000100000000000000000000
0100000000000010000000000000000000010000000000000000000
0010000000000001000000000000000000001000000000000000000
0001000000000000100000000000000000000100000000000000000
0000100000000000010000000000000000000010000000000000000
0000010000000000001000000000000000000001000000000000000
0000001000000000000100000000000000000000100000000000000
0000000100000000000010000000000000000000010000000000000
0000000010000000000001000000000000000000001000000000000
0000000001000000000000100000000000000000000100000000000
0000000000100000000000010000000000000000000010000000000
0000000000010000000000001000000000000000000001000000000
0000000000001000000000000100000000000000000000100000000
0000000000000100000000000010000000000000000000010000000
0000000000000010000000000001000000000000000000001000000
0000000000000001000000000000100000000000000000000100000
0000000000000000100000000000010000000000000000000010000
0000000000000000010000000000001000000000000000000001000
0000000000000000001000000000000100000000000000000000100
0000000000000000000100000000000010000000000000000000010
0000000000000000000010000000000001000000000000000000001
1000000000000000000000000000000000100000000000000000000
0100000000000000000000000000000000010000000000000000000
0010000000000000000000000000000000001000000000000000000
0001000000000000000000000000000000000100000000000000000
0000100000000000000000000000000000000010000000000000000
0000010000000000000000000000000000000001000000000000000
0000001000000000000000000000000000000000100000000000000
0000000100000000000000000000000000000000010000000000000
0000000010000000000000000000000000000000001000000000000
0000000001000000000000000000000000000000000100000000000
0000000000100000000000000000000000000000000010000000000
0000000000010000000000000000000000000000000001000000000
0000000000001000000000000000000000000000000000100000000
0000000000000100000000000000000000000000000000010000000
0000000000000010000000000000000000000000000000001000000
0000000000000001000000000000000000000000000000000100000
0000000000000000100000000000000000000000000000000010000
0000000000000000010000000000000000000000000000000001000
0000000000000000001000000000000000000000000000000000100
0000000000000000000100000000000000000000000000000000010
0000000000000000000010000000000000000000000000000000001
*/
            uint[,] matrix = new uint[55, 55];

            for (int i = 0; i < 13; i++)
            {
                matrix[i, i] = 1;
                matrix[i, 8 + i] = 1;
                matrix[i, 21 + i] = 1;
                matrix[i, 42 + i] = 1;
            }

            for (int i = 0; i < 21; i++)
            {
                matrix[13 + i, i] = 1;
                matrix[13 + i, 13 + i] = 1;
                matrix[13 + i, 34 + i] = 1;
            }

            for (int i = 0; i < 21; i++)
            {
                matrix[34 + i, i] = 1;
                matrix[34 + i, 34 + i] = 1;
            }

            return matrix;
        } //PrngDotNetBase.GeneratePreviousMatrix

        private static void MultiplyMatrix(uint[,] destination, uint[,] a, uint[,] b)
        {
            for (int i = 0; i < 55; i++)
            {
                for (int j = 0; j < 55; j++)
                {
                    uint accumulator = 0;

                    for (int k = 0; k < 55; k++)
                    {
                        accumulator += unchecked((uint)(((ulong)a[i, k] * (ulong)b[k, j]) % MBIG));
                        if (accumulator >= MBIG) accumulator -= MBIG;
                    }

                    destination[i, j] = accumulator;
                }
            }
        } //PrngDotNetBase.MultiplyMatrix

    } //class PrngDotNetBase

} //namespace Cylance.Research.Prangster
