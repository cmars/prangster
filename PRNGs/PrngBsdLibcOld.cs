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

// Based on FreeBSD rand.c, which bears the following copyright notice and conditions:
/*-
 * Copyright (c) 1990, 1993
 *    The Regents of the University of California.  All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 4. Neither the name of the University nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE REGENTS AND CONTRIBUTORS ``AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE REGENTS OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 * OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 */

using System;

namespace Cylance.Research.Prangster
{

    public sealed class PrngBsdLibcOld : PrngLcgBase
    {

        private uint _State;

        private const uint _Multiplier = 1103515245;
        private const uint _Increment = 12345;
        private const uint _Modulus = 0x80000000;  // & 0x7FFFFFFF (31 bits)

        private const uint _MultiplierInverse = 1857678181;  // multiplicative inverse of _Multiplier modulo _Modulus
        private const uint _IncrementInverseMulInv = 2088216195;  // additive inverse of _Increment modulo _Modulus, multiplied by _MultiplierInverse

        public PrngBsdLibcOld()
        {
        }

        public PrngBsdLibcOld(ulong seed)
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
                return 1;
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

        public override ulong Next()
        {
            unchecked  // disable arithmetic checks for performance
            {
                uint next = (this._State * _Multiplier) + _Increment;
                this._State = next;
                return (next & 0x7FFFFFFF);
            }
        }

        public override ulong Next(ulong limit)
        {
            unchecked  // disable arithmetic checks for performance
            {
                uint next = (this._State * _Multiplier) + _Increment;
                this._State = next;

                next = (next & 0x7FFFFFFF);

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

        private static uint PreviousState(uint seed)
        {
            unchecked  // disable arithmetic checks for performance
            {
                uint diff = (seed - _Increment);
                return (diff * _MultiplierInverse) & 0x7FFFFFFF;  // we can let multiplication overflow 32 bits because modulus is a power of two
            }
        }

        public override ulong Previous()
        {
            uint current = this._State;
            this._State = PreviousState(current);

            return (current & 0x7FFFFFFF);
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
            this._State = unchecked((uint)SeekState(this._State, offset, _Multiplier, _Increment, _Modulus));
        }

        public override void SeekBack(ulong offset)
        {
            this._State = unchecked((uint)SeekState(this._State, offset, _MultiplierInverse, _IncrementInverseMulInv, _Modulus));
        }

        public override ulong SeekSeedAhead(ulong seed, ulong offset)
        {
            return unchecked((uint)SeekState(seed, offset, _Multiplier, _Increment, _Modulus));
        }

        public override ulong SeekSeedBack(ulong seed, ulong offset)
        {
            return unchecked((uint)SeekState(seed, offset, _MultiplierInverse, _IncrementInverseMulInv, _Modulus));
        }

        protected override bool RecoverSeed(ulong seedStart, ulong seedEnd, ulong seedIncrement, ulong[] output, ulong limit, ulong wildcard, RecoverSeedCallback callback, ulong progressInterval)
        {
            if (output.Length < 2 || output[0] >= limit || output[0] == wildcard || limit == 0 || limit > _Modulus || seedIncrement != 1)
                return base.RecoverSeed(seedStart, seedEnd, seedIncrement, output, limit, wildcard, callback, progressInterval);

            if (seedStart < this.MinimumSeed)
                seedStart = this.MinimumSeed;

            if (seedEnd > this.MaximumSeed)
                seedEnd = this.MaximumSeed;

            if (seedStart > seedEnd)
                return false;

            this._OriginalCallback = callback;

            unchecked  // disable arithmetic checks for performance
            {
                // operate on output suffix of length N - 1 so we can fix bottom portion of seed to output[0]
                ulong[] suboutput = new ulong[output.Length - 1];
                Array.Copy(output, 1, suboutput, 0, output.Length - 1);

                uint ofs = unchecked(((uint)limit + (uint)output[0] - ((uint)seedStart % (uint)limit)) % (uint)limit);
                if (ofs != 0) seedStart += ofs;  // advance 'seedStart' to first value in range that produces output[0] modulo 'limit'

                // pass 'limit' instead of 'seedIncrement' (1), skipping all seeds that don't (modulo 'limit') equal output[0]
                return base.RecoverSeed(seedStart, seedEnd, limit, suboutput, limit, wildcard, this.RecoverSeedCallback, progressInterval);
            } //unchecked
        } //PrngBsdLibc.RecoverSeed

        private RecoverSeedCallback _OriginalCallback;

        private void RecoverSeedCallback(RecoverSeedEventArgs args)
        {
            if (args.EventType == RecoverSeedEventType.SeedDiscovered)
                args.Seed = PreviousState(unchecked((uint)args.Seed));

            this._OriginalCallback(args);
        }

    } //class PrngBsdLibcOld

} //namespace Cylance.Research.Prangster
