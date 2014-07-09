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

    public sealed class PrngBsdLibc : PrngLehmerBase
    {

        private uint _State;

        private const uint _Multiplier = 16807;
        private const uint _Modulus = 0x7FFFFFFF;

        private const uint _MultiplierInverse = 1407677000;  // multiplicative inverse of _Multiplier modulo _Modulus

        public PrngBsdLibc()
        {
        }

        public PrngBsdLibc(ulong seed)
            : this()
        {
            this.Seed(seed);
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
            //if (seed < 1 || seed > (_Modulus - 1))
            //    throw new ArgumentOutOfRangeException("seed must be a positive 32-bit integer");

            unchecked  // disable arithmetic checks for performance
            {
                this._State = (uint)seed % _Modulus;
            }
        }

        public override ulong Next()
        {
            unchecked  // disable arithmetic checks for performance
            {
                return (this._State = (uint)(((ulong)this._State * (ulong)_Multiplier) % (ulong)_Modulus));
            }
        }

        public override ulong Next(ulong limit)
        {
            unchecked  // disable arithmetic checks for performance
            {
                uint next = (uint)this.Next();

                if (limit != 0 && limit < _Modulus)
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

        private static uint PreviousState(uint state)
        {
            return unchecked((uint)(((ulong)state * _MultiplierInverse) % _Modulus));
        }

        public override ulong Previous()
        {
            uint current = this._State;
            this._State = PreviousState(current);

            return current;
        }

        public override ulong Previous(ulong limit)
        {
            unchecked  // disable arithmetic checks for performance
            {
                uint current = (uint)this.Previous();

                if (limit != 0 && limit < _Modulus)
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
            this._State = unchecked((uint)SeekState(this._State, offset, _Multiplier, _Modulus));
        }

        public override void SeekBack(ulong offset)
        {
            this._State = unchecked((uint)SeekState(this._State, offset, _MultiplierInverse, _Modulus));
        }

        public override ulong SeekSeedAhead(ulong seed, ulong offset)
        {
            return SeekState(seed, offset, _Multiplier, 0, _Modulus);
        }

        public override ulong SeekSeedBack(ulong seed, ulong offset)
        {
            return SeekState(seed, offset, _MultiplierInverse, 0, _Modulus);
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

    } //class PrngBsdLibc

} //namespace Cylance.Research.Prangster
