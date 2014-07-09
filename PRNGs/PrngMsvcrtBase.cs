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

// Based on Microsoft Visual Studio 2012 CRT rand.c, which bears the following copyright notice:
//   Copyright (c) 2006 Microsoft Corporation.  All rights reserved.

using System;

namespace Cylance.Research.Prangster
{

    public abstract class PrngMsvcrtBase : PrngLcgBase
    {

        private uint _State;

        protected const uint _Multiplier = 214013;
        protected const uint _Increment = 2531011;
        protected const uint _Modulus = 0x80000000;  // 31 bits (technically 32 bits, but MSB never influences output)
        protected const uint _OutputDivisor = 0x8000;  // & 0x7FFF
        protected const uint _DiscardDivisor = 0x10000;  // >> 16

        private const uint _MultiplierInverse = 968044885;  // multiplicative inverse of _Multiplier modulo _Modulus
        private const uint _IncrementInverseMulInv = 561051201;  // additive inverse of _Increment modulo _Modulus, multiplied by _MultiplierInverse

        public PrngMsvcrtBase()
        {
        }

        public PrngMsvcrtBase(ulong seed)
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
                return (_Modulus - 1);
            }
        }

        public override ulong Multiplier
        {
            get
            {
                return _Multiplier;
            }
        }

        public override ulong Increment
        {
            get
            {
                return _Increment;
            }
        }

        public override ulong Modulus
        {
            get
            {
                return _Modulus;
            }
        }

        public override ulong DiscardDivisor
        {
            get
            {
                return _DiscardDivisor;
            }
        }

        public override ulong OutputDivisor
        {
            get
            {
                return _OutputDivisor;
            }
        }

        public override void Seed(ulong seed)
        {
            //if (seed < 0 || seed > (_Modulus - 1))
            //    throw new ArgumentOutOfRangeException("seed must be a 31-bit integer");

            unchecked  // disable arithmetic checks for performance
            {
                this._State = ((uint)seed & 0x7FFFFFFF);
            }
        }

        protected static uint NextState(uint state)
        {
            return unchecked(((state * _Multiplier) + _Increment) & 0x7FFFFFFF);
        }

        protected static uint NextFromState(uint state)
        {
            return (PrngMsvcrtBase.NextState(state) >> 16);  // & 0x7FFF
        }

        public override ulong Next()
        {
            uint next = PrngMsvcrtBase.NextState(this._State);
            this._State = next;
            return (next >> 16);  // & 0x7FFF
        }

        public override bool CanReverse
        {
            get
            {
                return true;
            }
        }

        protected static uint PreviousState(uint state)
        {
            unchecked  // disable arithmetic checks for performance
            {
                uint diff = (state - _Increment);
                return (diff * _MultiplierInverse) & 0x7FFFFFFF;  // we can let multiplication overflow 32 bits because modulus is a power of two
            }
        }

        public override ulong Previous()
        {
            uint current = this._State;
            this._State = PreviousState(current);
            return (current >> 16);  // & 0x7FFF
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
            this._State = unchecked((uint)SeekState(this._State, offset, _Multiplier, _Increment, _Modulus));
        }

        public override void SeekBack(ulong offset)
        {
            this._State = unchecked((uint)SeekState(this._State, offset, _MultiplierInverse, _IncrementInverseMulInv, _Modulus));
        }

        public override ulong SeekSeedAhead(ulong seed, ulong offset)
        {
            return SeekState(seed, offset, _Multiplier, _Increment, _Modulus);
        }

        public override ulong SeekSeedBack(ulong seed, ulong offset)
        {
            return SeekState(seed, offset, _MultiplierInverse, _IncrementInverseMulInv, _Modulus);
        }

    } //class PrngMsvcrtBase

} //namespace Cylance.Research.Prangster
