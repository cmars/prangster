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

// Implements the algorithm of Oracle JDK 7 Random.java, which bears the following copyright notice:
//   Copyright (c) 1995, 2010, Oracle and/or its affiliates. All rights reserved.

using System;

namespace Cylance.Research.Prangster
{

    public sealed class PrngJava : PrngLcgBase
    {

        private const ulong _Multiplier = 0x5DEECE66D;
        private const ulong _Increment = 0xB;
        private const ulong _Modulus = 0x1000000000000;  // & 0x0000FFFF`FFFFFFFF (48 bits)
        private const ulong _DiscardDivisor = 0x10000;  // >> 16 for Next()
        private const ulong _DiscardDivisorForMod = 0x20000;  // >> 17 for Next(ulong) where limit is not a power of two
        private const ulong _OutputDivisor = 0x100000000;  // (32 bits)

        private const ulong _MultiplierInverse = 0xDFE05BCB1365;  // multiplicative inverse of _Multiplier modulo _Modulus

        public PrngJava()
        {
        }

        public PrngJava(ulong seed)
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
            //if (seed > (_Modulus - 1))
            //    throw new ArgumentOutOfRangeException("seed must be a non-negative 48-bit integer");

            unchecked  // disable arithmetic checks for performance
            {
                this._LcgState = (seed ^ _Multiplier) & 0x0000FFFFFFFFFFFF;
            }
        }

        protected override ulong ValueFromRaw(ulong raw)
        {
            return (raw >> 16);
        }

        protected override ulong ValueFromRaw(ulong raw, ulong limit)
        {
            unchecked  // disable arithmetic checks for performance
            {
                if ((limit & (limit - 1)) == 0)  // special handling for power-of-two limits
                {
                    return ((raw >> 17) * limit) >> 31;  // keeps only most-significant bits
                }
                else
                {
                    return (raw >> 17) % limit;
                }
            }
        }

        public override ulong Next()  // Random.nextInt()
        {
            unchecked  // disable arithmetic checks for performance
            {
                ulong next = ((this._LcgState * _Multiplier) + _Increment) & 0x0000FFFFFFFFFFFF;
                this._LcgState = next;
                return (next >> 16);
            }
        }

        public override ulong Next(ulong limit)  // Random.nextInt(int)
        {
            //if (limit <= 0 || limit > int.MaxValue)
            //    throw new ArgumentOutOfRangeException("limit must be a positive 32-bit integer");

            unchecked  // disable arithmetic checks for performance
            {
                if ((limit & (limit - 1)) == 0)  // special handling for power-of-two limits
                {
                    ulong next = ((this._LcgState * _Multiplier) + _Increment) & 0x0000FFFFFFFFFFFF;
                    this._LcgState = next;

                    return ((next >> 17) * limit) >> 31;  // keeps only most-significant bits
                }
                else
                {
                    ulong next, value;

                    do  // loop to skip states that would otherwise slightly bias distribution toward the lowest (limit % Modulus) values
                    {
                        next = ((this._LcgState * _Multiplier) + _Increment) & 0x0000FFFFFFFFFFFF;
                        this._LcgState = next;

                        next >>= 17;  // (48 - 17) = 31-bit (non-negative integer) result
                        value = (next % limit);
                    }
                    while (next - value + (limit - 1) >= 0x80000000);

                    return value;
                }
            }
        }

        public override bool CanReverse
        {
            get
            {
                return true;
            }
        }

        private static ulong PreviousState(ulong state)
        {
            unchecked  // disable arithmetic checks for performance
            {
                ulong diff = (state - _Increment);
                return (diff * _MultiplierInverse) & 0x0000FFFFFFFFFFFF;  // we can let multiplication overflow 64 bits because modulus is a power of two
            }
        }

        public override ulong Previous()
        {
            ulong current = this._LcgState;

            this._LcgState = PreviousState(this._LcgState);

            return (current >> 16);
        }

        public override ulong Previous(ulong limit)
        {
            //if (limit <= 0 || limit > int.MaxValue)
            //    throw new ArgumentOutOfRangeException("limit must be a positive 32-bit integer");

            ulong current = this._LcgState;

            unchecked  // disable arithmetic checks for performance
            {
                if ((limit & (limit - 1)) == 0)  // special handling for power-of-two limits
                {
                    this._LcgState = PreviousState(this._LcgState);

                    return ((current >> 17) * limit) >> 31;  // keeps only most-significant bits
                }
                else
                {
                    ulong prev, value;

                    do  // loop to skip states that would otherwise slightly bias distribution toward the lowest (limit % Modulus) values
                    {
                        this._LcgState = PreviousState(this._LcgState);

                        prev = (this._LcgState >> 17);  // (48 - 17) = 31-bit (non-negative integer) result
                        value = (prev % limit);
                    }
                    while (prev - value + (limit - 1) >= 0x80000000);

                    return (current >> 17) % limit;  // this shouldn't fall within the bias zone, but it could; currently we choose to ignore that case
                }
            }
        }

        public override ulong SeekSeedAhead(ulong seed, ulong offset)
        {
            return (base.SeekSeedAhead(seed ^ _Multiplier, offset) ^ _Multiplier);
        }

        public override ulong SeekSeedBack(ulong seed, ulong offset)
        {
            return (base.SeekSeedBack(seed ^ _Multiplier, offset) ^ _Multiplier);
        }

        protected override bool RecoverSeed(ulong seedStart, ulong seedEnd, ulong seedIncrement, ulong[] output, ulong limit, ulong wildcard, RecoverSeedCallback callback, ulong progressInterval)
        {
            if (output.Length < 2 || output[0] > (ulong.MaxValue / _OutputDivisor) || output[0] == wildcard || limit == 0 || limit > _OutputDivisor)
                return base.RecoverSeed(seedStart, seedEnd, seedIncrement, output, limit, wildcard, callback, progressInterval);

            if (seedStart > seedEnd || seedIncrement == 0)
                return false;

            if (unchecked(limit & (limit - 1)) == 0)  // special handling for power-of-two limits
            {
                return this.RecoverSeedPowerOf2(seedStart, seedEnd, seedIncrement, output, limit, wildcard, callback, progressInterval);
            }

            RecoverSeedEventArgs args = new RecoverSeedEventArgs();

            unchecked  // disable arithmetic checks for performance
            {
                // operate on output suffix of length N - 1 so we can fix middle portion of seed to output[0]
                ulong[] suboutput = new ulong[output.Length - 1];
                Array.Copy(output, 1, suboutput, 0, output.Length - 1);

                ulong seedtopincr = (uint)limit * _DiscardDivisorForMod;

                // Modulus is a power of 2, so GCD(limit, Modulus) is greatest power-of-two factor of limit
                ulong subgenoutputmask = ((uint)limit ^ ((uint)limit - 1)) >> 1;

                // fix bottom portion by brute-forcing subgenerator
                ulong subseed, subseedend;
                for (subseed = (output[0] & subgenoutputmask) * _DiscardDivisorForMod, subseedend = subseed + _DiscardDivisorForMod - 1; subseed != subseedend; subseed++)
                {
                    if (subgenoutputmask != 0)  // if limit is odd, we can't check this subgenerator seed against LSB(s) of output; we must test all possibilities below
                    {
                        this._LcgState = subseed;  // don't use Seed() because it XORs by _Multiplier

                        int i;
                        for (i = 0; i < suboutput.Length; i++)
                        {
                           if ((this.Next(limit) & subgenoutputmask) != (suboutput[i] & subgenoutputmask) && suboutput[i] != wildcard)
                                break;
                        }

                        if (i < suboutput.Length)
                            continue;
                    }

                    // MSB(s) of subseed will contain LSB(s) of output[0], so | instead of + to keep duplicate bit(s) from interfering
                    for (ulong seed = (output[0] * _DiscardDivisorForMod) | subseed; seed <= seedEnd; seed += seedtopincr)
                    {
                        this._LcgState = seed;  // don't use Seed() because it XORs by _Multiplier

                        int i;
                        for (i = 0; i < suboutput.Length; i++)
                        {
                            if (this.Next(limit) != suboutput[i] && suboutput[i] != wildcard)
                                break;
                        }

                        if (i == suboutput.Length)  // invoke callback for seed discovery notification
                        {
                            args.EventType = RecoverSeedEventType.SeedDiscovered;
                            args.Seed = (PreviousState(seed) ^ _Multiplier);
                            // TO-DO TODO: estimate progress/total somehow?
                            args.CurrentAttempts = 0;
                            args.TotalAttempts = 0;

                            callback(args);

                            if (args.Cancel)
                                return false;
                        }
                    } //for(seed<=seedEnd)
                } //for(subseed<subgensize)
            } //unchecked

            return true;
        } //PrngJava.RecoverSeed

        private bool RecoverSeedPowerOf2(ulong seedStart, ulong seedEnd, ulong seedIncrement, ulong[] output, ulong limit, ulong wildcard, RecoverSeedCallback callback, ulong progressInterval)
        {
            this._OriginalCallback = callback;

            unchecked  // disable arithmetic checks for performance
            {
                // operate on output suffix of length N - 1 so we can fix top portion of seed to output[0]
                ulong[] suboutput = new ulong[output.Length - 1];
                Array.Copy(output, 1, suboutput, 0, output.Length - 1);

                ulong rangesize = (1UL << 48) / limit;
                ulong rangestart = (output[0] * rangesize);
                ulong rangeend = rangestart + rangesize - 1;

                if (rangestart <= seedStart)
                {
                    rangestart = seedStart;
                }
                else if (seedIncrement > 1)
                {
                    ulong offset = (rangestart - seedStart) % seedIncrement;
                    if (offset != 0)
                        rangestart += (seedIncrement - offset);  // advance to the next multiple of 'seedIncrement' (relative to 'seedStart')
                }

                if (rangeend > seedEnd)
                    rangeend = seedEnd;

                return base.RecoverSeed(rangestart, rangeend, seedIncrement, suboutput, limit, wildcard, this.RecoverSeedPowerOf2Callback, progressInterval);
            } //unchecked
        } //PrngJava.RecoverSeedPowerOf2

        private RecoverSeedCallback _OriginalCallback;

        private void RecoverSeedPowerOf2Callback(RecoverSeedEventArgs args)
        {
            if (args.EventType == RecoverSeedEventType.SeedDiscovered)
                args.Seed = PreviousState(args.Seed ^ _Multiplier) ^ _Multiplier;

            this._OriginalCallback(args);
        }

    } //class PrngJava

} //namespace Cylance.Research.Prangster
