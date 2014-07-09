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

// Implements the MWC algorithm from Google V8 v8.cc, which is subject to the following copyright and conditions:
//
// Copyright 2012 the V8 project authors. All rights reserved.
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
//       copyright notice, this list of conditions and the following
//       disclaimer in the documentation and/or other materials provided
//       with the distribution.
//     * Neither the name of Google Inc. nor the names of its
//       contributors may be used to endorse or promote products derived
//       from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;

namespace Cylance.Research.Prangster
{

    public sealed class PrngV8 : PrngBase
    {

        private const uint _Multiplier0 = 18273;
        private const uint _Multiplier1 = 36969;

        private uint _State0, _State1;

        public PrngV8()
        {
        }

        public PrngV8(ulong seed)
            : this()
        {
            this.Seed(seed);
        }

        public override ulong MinimumSeed
        {
            get
            {
                // random_base depends on both state variables being non-zero,
                // although it doesn't appear to check the output of the entropy source;
                // yes, this minimum still allows the low uint to be zero for high uints > 1
                return 0x0000000100000001;
            }
        }

        public override ulong MaximumSeed
        {
            get
            {
                return ulong.MaxValue;
            }
        }

        public override void Seed(ulong seed)
        {
            //if ((seed & 0x00000000FFFFFFFFUL) == 0 || (seed & 0xFFFFFFFF00000000UL) == 0)
            //    throw new ArgumentOutOfRangeException("seed must comprise two non-zero 32-bit integers");

            unchecked  // disable arithmetic checks for performance
            {
                this._State0 = (uint)(seed >> 32);
                this._State1 = (uint)seed;
            }
        }

        public override ulong Next()
        {
            unchecked  // disable arithmetic checks for performance
            {
                this._State0 = (_Multiplier0 * (this._State0 & 0xFFFF)) + (this._State0 >> 16);
                this._State1 = (_Multiplier1 * (this._State1 & 0xFFFF)) + (this._State1 >> 16);

                return (uint)((this._State0 << 14) + (this._State1 & 0x3FFFF));
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

        private static uint PreviousState0(uint state)
        {
            // many states have multiple possible previous states; we just do the best we can

            if (state >= (_Multiplier0 * 0x10000) - 1)
            {
                if (state <= (_Multiplier0 * 0xFFFF) + 0xFFFF)
                    return unchecked(((state - (_Multiplier0 * 0xFFFF)) << 16) | 0xFFFF);
                else throw new ArgumentOutOfRangeException("No previous state exists.");
            }

            return unchecked(((state % _Multiplier0) << 16) | (state / _Multiplier0));
        }

        private static uint PreviousState1(uint state)
        {
            // many states have multiple possible previous states; we just do the best we can

            if (state >= (_Multiplier1 * 0x10000) - 1)
            {
                if (state <= (_Multiplier1 * 0xFFFF) + 0xFFFF)
                    return unchecked(((state - (_Multiplier1 * 0xFFFF)) << 16) | 0xFFFF);
                else throw new ArgumentOutOfRangeException("No previous state exists.");
            }

            return unchecked(((state % _Multiplier1) << 16) | (state / _Multiplier1));
        }

        public override ulong Previous()
        {
            uint current0 = this._State0;
            uint current1 = this._State1;

            this._State0 = PreviousState0(current0);
            this._State1 = PreviousState1(current1);

            return (uint)((current0 << 14) + (current1 & 0x3FFFF));
        }

        public override ulong Previous(ulong limit)
        {
            //if (limit <= 0 || limit > int.MaxValue)
            //    throw new ArgumentOutOfRangeException("limit must be a positive 32-bit integer");

            return unchecked((this.Previous() * limit) >> 32);
        }

        protected override bool RecoverSeed(ulong seedStart, ulong seedEnd, ulong seedIncrement, ulong[] output, ulong limit, ulong wildcard, RecoverSeedCallback callback, ulong progressInterval)
        {
            /// TO-DO TODO: make this more accurate in general, account for off-by-one (esp. in output[0] position), etc.

            if (output.Length < 2 || output[0] > uint.MaxValue || output[0] == wildcard || limit == 0 || limit > (ulong)uint.MaxValue + 1 || seedIncrement != 1)
                return base.RecoverSeed(seedStart, seedEnd, seedIncrement, output, limit, wildcard, callback, progressInterval);

            if (seedStart > seedEnd)
                return false;

            if (callback == null)
                progressInterval = 0;

            ulong output1 = ((output.Length > 2) ? output[1] : wildcard);

            uint blocksize = (uint)(0x100000000 / ((ulong)0x10000 * limit));

            RecoverSeedEventArgs args = new RecoverSeedEventArgs();

            unchecked  // disable arithmetic checks for performance
            {
                // operate on output suffix of length N - 1 so we can fix middle portion of seed to output[0]
                ulong[] suboutput = new ulong[output.Length - 1];
                Array.Copy(output, 1, suboutput, 0, output.Length - 1);

                // lowstart..lowend define the range of values that the low 18 bits of State0 could have taken after producing the observed output[0]
                uint lowstart = (uint)((output[0] * 0x100000000 + (limit - 1)) / limit) >> 14;
                uint lowend = (uint)((((output[0] + 1) * 0x100000000 + (limit - 1)) / limit) - 1) >> 14;

                ulong totalcount = (ulong)(lowend - lowstart) * (((ulong)_Multiplier0 / 4) + 1);
                ulong totalcountdown = totalcount;

                ulong countdown = ((totalcountdown < progressInterval || progressInterval == 0) ? totalcountdown : progressInterval);

                int skip = (output1 == wildcard) ? -1 : 0;
                if (blocksize < 2) skip = -1;  // don't attempt improved attack if block size is too small

                for (uint statelow = lowstart; statelow < lowend; statelow++)
                {
                    ulong rangestart = ((ulong)statelow << 32) | 1;
                    ulong rangeend = rangestart + (((ulong)_Multiplier0 / 4) << 50);

                    for (ulong seed = rangestart; seed <= rangeend; )
                    {
                        uint rounds = (uint)(((rangeend - seed) >> 50) + 1);
                        if (rounds > countdown) rounds = (uint)countdown;

                        uint roundnum = 0;

                        if (skip > 0)
                        {
                            if ((uint)skip >= rounds)
                            {
                                seed += 0x4000000000000 * (ulong)rounds;
                                skip -= (int)(rounds - roundnum);
                                roundnum = rounds;
                            }
                            else
                            {
                                roundnum = (uint)skip;
                                seed += 0x4000000000000 * (ulong)(uint)skip;
                                skip = 0;
                            }
                        }

                        // we have the low 18 bits of State0 confined to a range, but we still need to brute-force the top 14 bits (up to Multiplier0 / 4)
                        for (; roundnum < rounds; seed += 0x4000000000000, roundnum++)
                        {
                            this.Seed(seed);

                            uint o1test = (uint)this.Next(limit);
                            if (o1test != output1 && output1 != wildcard)
                            {
                                if (skip == 0)
                                {
                                    uint diff = ((uint)output1 + (uint)limit - o1test) % (uint)limit;
                                    if (diff > 1)
                                    {
                                        skip = (int)((diff - 1) * blocksize);

                                        if ((uint)skip >= (rounds - roundnum))
                                        {
                                            seed += 0x4000000000000 * (ulong)(rounds - roundnum);
                                            skip -= (int)(rounds - roundnum);
                                            roundnum = rounds;
                                            break;
                                        }

                                        roundnum += (uint)(skip - 1);
                                        seed += 0x4000000000000 * (ulong)(uint)(skip - 1);
                                        skip = 0;
                                    }
                                }
                                continue;
                            }
                            else if (skip > 0) skip = 0;

                            int i;
                            for (i = 1; i < suboutput.Length; i++)
                            {
                                if (this.Next(limit) != suboutput[i] && suboutput[i] != wildcard)
                                    break;
                            }

                            if (i >= suboutput.Length)  // invoke callback for seed discovery notification
                            {
                                args.EventType = RecoverSeedEventType.SeedDiscovered;
                                args.Seed = unchecked(((ulong)PreviousState0((uint)(seed >> 32)) << 32) | PreviousState1((uint)seed));
                                args.CurrentAttempts = (totalcount - totalcountdown + roundnum);
                                args.TotalAttempts = totalcount;

                                callback(args);

                                if (args.Cancel)
                                    return false;
                            }
                        } //for(roundnum<rounds)

                        totalcountdown -= rounds;
                        countdown -= rounds;

                        if (countdown == 0 && progressInterval != 0)
                        {
                            args.EventType = RecoverSeedEventType.ProgressNotification;
                            args.Seed = seed;
                            args.CurrentAttempts = (totalcount - totalcountdown);
                            args.TotalAttempts = totalcount;

                            callback(args);

                            if (args.Cancel)
                                return false;

                            countdown = ((totalcountdown < progressInterval) ? totalcountdown : progressInterval);
                        }
                    } //for(seed<=rangeend)
                } //for(statelow<lowend)
            } //unchecked

            return true;
        } //PrngV8.RecoverSeed

    } //class PrngV8

} //namespace Cylance.Research.Prangster
