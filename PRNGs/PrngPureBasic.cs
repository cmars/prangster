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

    /// <summary>
    /// PureBasic (RandomSeed and Random functions) PRNG.
    /// </summary>
    /// <remarks>
    /// Appears to be based on Agner Fog's RANROT generator type B (see http://www.agner.org/random/discuss/read.php?i=138).
    /// </remarks>
    public sealed class PrngPureBasic : PrngBase
    {

        private const int KK = 17;

        private readonly uint[] _RandBuffer1 = new uint[KK];
        private readonly uint[] _RandBuffer2 = new uint[KK];
        private int _p1, _p2;

        public PrngPureBasic()
        {
        }

        public PrngPureBasic(ulong seed)
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
                return uint.MaxValue;
            }
        }

        public override void Seed(ulong seed)
        {
            //if (seed > uint.MaxValue)
            //    throw new ArgumentOutOfRangeException("seed must be a 32-bit integer");

            unchecked  // disable arithmetic checks for performance
            {
                uint s = (uint)seed;

                for (int i = 0; i < KK; i++)
                {
                    s = (s * 2891336453) + 1;  // this multiplier has been attributed to Pierre L'Ecuyer
                    this._RandBuffer1[i] = s;
                    s = (s * 2891336453) + 1;
                    this._RandBuffer2[i] = s;
                }

                this._p1 = 0;
                this._p2 = 10;

                for (int i = 0; i < 31; i++)
                    this.Next();
            }
        }

        public override ulong Next()
        {
            unchecked  // disable arithmetic checks for performance
            {
                uint x1 = this._RandBuffer1[this._p1];
                x1 = ((x1 >> 13) | (x1 << 19)) + this._RandBuffer1[this._p2];

                uint x2 = this._RandBuffer2[this._p1];
                x2 = ((x2 >> 5) | (x2 << 27)) + this._RandBuffer2[this._p2];

                this._RandBuffer1[this._p1] = x2;
                this._RandBuffer2[this._p1] = x1;

                this._p1 = (this._p1 == 0 ? KK : this._p1) - 1;
                this._p2 = (this._p2 == 0 ? KK : this._p2) - 1;

                return x1;
            }
        }

        public override ulong Next(ulong limit)
        {
            //if (limit <= 0 || limit > int.MaxValue)
            //    throw new ArgumentOutOfRangeException("limit must be a positive 32-bit integer");

            return unchecked((this.Next() * limit) >> 32);
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
                if (++this._p1 == KK) this._p1 = 0;
                if (++this._p2 == KK) this._p2 = 0;

                uint x1 = this._RandBuffer2[this._p1];
                uint x2 = this._RandBuffer1[this._p1];
                uint current = x1;

                x1 -= this._RandBuffer1[this._p2];
                this._RandBuffer1[this._p1] = ((x1 << 13) | (x1 >> 19));

                x2 -= this._RandBuffer2[this._p2];
                this._RandBuffer2[this._p1] = ((x2 << 5) | (x2 >> 27));

                return current;
            }
        }

        public override ulong Previous(ulong limit)
        {
            //if (limit <= 0 || limit > int.MaxValue)
            //    throw new ArgumentOutOfRangeException("limit must be a positive 32-bit integer");

            return unchecked((this.Previous() * limit) >> 32);
        }

    } //class PrngPureBasic

} //namespace Cylance.Research.Prangster
