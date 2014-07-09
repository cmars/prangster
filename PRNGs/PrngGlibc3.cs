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

// Based on GNU C Library (glibc) random_r.cs, which bears the following copyright notices and conditions:
/*
   Copyright (C) 1995, 2005, 2009 Free Software Foundation

   The GNU C Library is free software; you can redistribute it and/or
   modify it under the terms of the GNU Lesser General Public
   License as published by the Free Software Foundation; either
   version 2.1 of the License, or (at your option) any later version.

   The GNU C Library is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
   Lesser General Public License for more details.

   You should have received a copy of the GNU Lesser General Public
   License along with the GNU C Library; if not, see
   <http://www.gnu.org/licenses/>. */

/*
   Copyright (C) 1983 Regents of the University of California.
   All rights reserved.

   Redistribution and use in source and binary forms, with or without
   modification, are permitted provided that the following conditions
   are met:

   1. Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
   2. Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.
   4. Neither the name of the University nor the names of its contributors
      may be used to endorse or promote products derived from this software
      without specific prior written permission.

   THIS SOFTWARE IS PROVIDED BY THE REGENTS AND CONTRIBUTORS ``AS IS'' AND
   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
   IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
   ARE DISCLAIMED.  IN NO EVENT SHALL THE REGENTS OR CONTRIBUTORS BE LIABLE
   FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
   DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
   OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
   HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
   LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
   OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
   SUCH DAMAGE.
*/

using System;

namespace Cylance.Research.Prangster
{

    public sealed class PrngGlibc3 : PrngBase
    {

        private readonly uint[] _State = new uint[31];
        private int _fptr;
        private int _rptr;

        private readonly uint[,] _NextMatrix;
        private readonly uint[,] _PreviousMatrix;

        public PrngGlibc3()
        {
            this._NextMatrix = GenerateNextMatrix();
            this._PreviousMatrix = GeneratePreviousMatrix();
        }

        public PrngGlibc3(ulong seed)
            : this()
        {
            this.Seed(seed);
        }

        public override ulong MinimumSeed
        {
            get
            {
                return 1;
            }
        }

        public override ulong MaximumSeed
        {
            get
            {
                return uint.MaxValue;
            }
        }

        public override void Seed(ulong seed)
        {
            unchecked  // disable arithmetic checks for performance
            {
                this._State[0] = (uint)seed;

                for (int i = 1; i < 31; i++)
                {
                    this._State[i] = (uint)((16807 * (ulong)this._State[i - 1]) % 2147483647);
                }
            }

            this._fptr = 3;
            this._rptr = 0;

            for (int i = 0; i < 310; i++)
                this.Next();
        }

        public override ulong Next()
        {
            unchecked  // disable arithmetic checks for performance
            {
                uint next = (this._State[this._fptr] += this._State[this._rptr]);

                if (++this._fptr >= 31) this._fptr = 0;
                if (++this._rptr >= 31) this._rptr = 0;

                return (next >> 1);
            }
        }

        public override ulong Next(ulong limit)
        {
            unchecked  // disable arithmetic checks for performance
            {
                uint next = (uint)this.Next();

                if (limit != 0 && limit <= 0x7FFFFFFF)
                    next %= (uint)limit;

                return next;
            }
        }

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
                if (--this._fptr < 0) this._fptr = 30;
                if (--this._rptr < 0) this._rptr = 30;

                uint current = this._State[this._fptr];

                this._State[this._fptr] -= this._State[this._rptr];

                return (current >> 1);
            }
        }

        public override ulong Previous(ulong limit)
        {
            unchecked  // disable arithmetic checks for performance
            {
                uint current = (uint)this.Previous();

                if (limit != 0 && limit <= 0x7FFFFFFF)
                    current %= (uint)limit;

                return current;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        public override void SeekAhead(ulong offset)
        {
            if (offset < 31)
            {
                Step(unchecked((int)offset));
                return;
            }

            int steps;
            if (this._rptr != 0)
            {
                steps = (31 - this._rptr);
                Step(steps);

                offset -= unchecked((uint)steps);
            }

            ulong blockoffset = (offset / 31);

            steps = unchecked((int)(offset % 31));
            if (steps >= 16)
            {
                blockoffset++;  // advance one more block and then go back to reduce the number of steps (although this could be slower due to more matrix multiplications)
                steps -= 31;
            }

            if (blockoffset != 0)
            {
                BlockSeek(blockoffset, this._NextMatrix);
            }

            Step(steps);
        }

        public override void SeekBack(ulong offset)
        {
            if (offset < 31)
            {
                Step(unchecked(-(int)offset));
                return;
            }

            int steps;
            if (this._rptr != 0)
            {
                steps = this._rptr;
                Step(-steps);

                offset -= unchecked((uint)steps);
            }

            ulong blockoffset = (offset / 31);

            steps = unchecked(-(int)(offset % 31));
            if (steps <= -16)
            {
                blockoffset++;  // go back one more block and then advance to reduce the number of steps (although this could be slower due to more matrix multiplications)
                steps += 31;
            }

            if (blockoffset != 0)
            {
                BlockSeek(blockoffset, this._PreviousMatrix);
            }

            Step(steps);
        }

        private void Step(int offset)
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
        }

        private void BlockSeek(ulong blockOffset, uint[,] matrix)
        {
            // compute 'absoffset'-th power of 'stepmatrix'
            uint[,] matrixa = (uint[,])(matrix.Clone());
            uint[,] matrixb = new uint[31, 31];

            uint[,] seekmatrixa = new uint[31, 31];
            uint[,] seekmatrixb = new uint[31, 31];
            for (int i = 0; i < 31; i++)  // initialize seek matrix to identity matrix
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

            uint[] newstate = new uint[31];

            for (int i = 0; i < 31; i++)
            {
                uint accumulator = 0;

                for (int k = 0; k < 31; k++)
                {
                    accumulator += unchecked((uint)((ulong)seekmatrixa[i, k] * (ulong)this._State[k]));
                }

                newstate[i] = accumulator;
            }

            Array.Copy(newstate, 0, this._State, 0, 31);
        }

        private static uint[,] GenerateNextMatrix()
        {
            uint[,] matrix = new uint[31, 31];

            for (int i = 0; i < 31; i++)
                for (int j = i; j >= 0; j -= 3)
                    matrix[i, j] = 1;

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 31; j++)
                    matrix[i, j] += matrix[i + 28, j];

            return matrix;
        }

        private static uint[,] GeneratePreviousMatrix()
        {
            uint[,] matrix = new uint[31, 31];

            for (int j = 0; j < 28; j++)
            {
                matrix[j, j] = 1;
                matrix[j + 3, j] = unchecked((uint)-1);
            }

            for (int j = 28; j < 31; j++)
            {
                matrix[j - 28, j] = unchecked((uint)-1);
                matrix[j - 25, j] = 1;
                matrix[j, j] = 1;
            }

            return matrix;
        }

        private static void MultiplyMatrix(uint[,] destination, uint[,] a, uint[,] b)
        {
            for (int i = 0; i < 31; i++)
            {
                for (int j = 0; j < 31; j++)
                {
                    uint accumulator = 0;

                    for (int k = 0; k < 31; k++)
                    {
                        accumulator += unchecked((uint)((ulong)a[i, k] * (ulong)b[k, j]));
                    }

                    destination[i, j] = accumulator;
                }
            }
        }

    } //class PrngGlibc3

} //namespace Cylance.Research.Prangster
