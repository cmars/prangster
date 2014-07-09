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

    public sealed class PrngVbscript : PrngLcgBase
    {

        private uint _State;

        private const uint _Multiplier = 0x00FD43FD;
        private const uint _Increment = 0x00C39EC3;
        private const uint _Modulus = 0x01000000;  // & 0x00FFFFFF (24 bits)

        private const uint _MultiplierInverse = 0x00093155;  // multiplicative inverse of _Multiplier modulo _Modulus
        private const uint _IncrementInverseMulInv = 0x00CDF641;  // additive inverse of _Increment modulo _Modulus, multiplied by _MultiplierInverse

        public PrngVbscript()
        {
        }

        public PrngVbscript(ulong seed)
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
                return 0;
            }
        }

        public override void Seed(ulong seed)
        {
            //if (seed < 0 || seed > (_Modulus - 1))
            //    throw new ArgumentOutOfRangeException("seed must be a 24-bit integer");

            this._State = unchecked((uint)seed & 0x00FFFFFF);
        }

        public override ulong Next()
        {
            unchecked  // disable arithmetic checks for performance
            {
                uint next = ((this._State * _Multiplier) + _Increment) & 0x00FFFFFF;
                this._State = next;
                return next;
            }
        }

        public override ulong Next(ulong limit)
        {
            //if (limit <= 0 || limit > int.MaxValue)
            //    throw new ArgumentOutOfRangeException("limit must be a positive 32-bit integer");

            unchecked  // disable arithmetic checks for performance
            {
                uint next = unchecked((uint)this.Next());

                // TO-DO TODO: emulate float rounding/imprecision instead of doing float conversion;
                //             arises when 'limit' is not a power of two
                //return ((ulong)next * limit) >> 24;
                return (ulong)(((float)next * 0.000000059604645F) * (float)limit);  // 1/16777216
            }
        }

        private static uint PreviousState(uint state)
        {
            return unchecked(((state - _Increment) * _MultiplierInverse) & 0x00FFFFFF);
        }

        public override ulong Previous()
        {
            uint current = (this._State & 0x00FFFFFF);
            this._State = PreviousState(current);
            return current;
        }

        public override ulong Previous(ulong limit)
        {
            unchecked  // disable arithmetic checks for performance
            {
                uint current = unchecked((uint)this.Previous());

                // TO-DO TODO: emulate float rounding/imprecision instead of doing float conversion;
                //             arises when 'limit' is not a power of two
                return (ulong)(((float)current * 0.000000059604645F) * (float)limit);  // 1/16777216
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
            if (output.Length < 2 || output[0] > (ulong.MaxValue / 16777216) || output[0] == wildcard || limit == 0)
                return base.RecoverSeed(seedStart, seedEnd, seedIncrement, output, limit, wildcard, callback, progressInterval);

            if (seedStart > seedEnd || seedIncrement == 0)
                return false;

            unchecked  // disable arithmetic checks for performance
            {
                // operate on output suffix of length N - 1 so we can fix top portion of seed to output[0]
                ulong[] suboutput = new ulong[output.Length - 1];
                Array.Copy(output, 1, suboutput, 0, output.Length - 1);

                // undershoot 'rangestart' and overshoot 'rangeend', to compensate for 1/16777216 < 0.000000059604645 < 1/16777215
                ulong rangestart = ((16777215 * output[0]) + (uint)limit - 1) / (uint)limit;  // add (limit - 1) to round up some while remaining conservative
                ulong rangeend = (((16777216 * (output[0] + 1)) + (uint)limit - 1) / (uint)limit) - 1;  // lowest seed that could produce (output[0] + 1), then - 1

                if (rangestart <= seedStart)
                {
                    rangestart = seedStart;
                }
                else if (seedIncrement > 1)
                {
                    ulong incroffset = (rangestart - seedStart) % seedIncrement;
                    if (incroffset != 0)
                        rangestart += (seedIncrement - incroffset);  // advance to the next multiple of 'seedIncrement' (relative to 'seedStart')
                }

                if (rangeend > seedEnd)
                    rangeend = seedEnd;

                this._OriginalCallback = callback;
                return base.RecoverSeed(rangestart, rangeend, seedIncrement, suboutput, limit, wildcard, this.RecoverSeedCallback, progressInterval);
            } //unchecked
        } //PrngVbscript.RecoverSeed

        private RecoverSeedCallback _OriginalCallback;

        private void RecoverSeedCallback(RecoverSeedEventArgs args)
        {
            if (args.EventType == RecoverSeedEventType.SeedDiscovered)
                args.Seed = PreviousState(unchecked((uint)args.Seed));

            this._OriginalCallback(args);
        }

    } //class PrngVbscript

} //namespace Cylance.Research.Prangster
