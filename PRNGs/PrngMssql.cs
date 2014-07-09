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

    public sealed class PrngMssql : PrngBase
    {

        private const uint _Modulus = 2147483563;
        private const uint _Modulus1 = 2147483399;

        private const uint _Multiplier0 = 40014;
        private const uint _Multiplier1 = 40692;

        private const uint _Multiplier0Inverse = 2082061899;  // multiplicative inverse of _Multiplier0 modulo _Modulus
        private const uint _Multiplier1Inverse = 1481316021;  // multiplicative inverse of _Multiplier1 modulo _Modulus1

        private uint _State0, _State1;

        public PrngMssql()
        {
        }

        public PrngMssql(ulong seed)
            : this()
        {
            this.Seed(seed);
        }


        public override ulong MinimumSeed
        {
            get
            {
                return _Modulus1 + 1;
            }
        }

        public override ulong MaximumSeed
        {
            get
            {
                return ((ulong)_Modulus * (ulong)_Modulus1) - 1;
            }
        }

        public override void Seed(ulong seed)
        {
            unchecked  // disable arithmetic checks for performance
            {
                uint state0, state1;

                SeedToStates(seed, out state0, out state1);

                if (state0 < 1 || state0 >= _Modulus)
                {
                    //throw new ArgumentOutOfRangeException("seed must comprise two positive integers, the most significant being less than 2147483563");
                    state0 = 12345;
                }

                if (state1 < 1)
                {
                    //throw new ArgumentOutOfRangeException("seed must comprise two positive integers, the least significant being less than 2147483399");
                    state1 = 67890;
                }

                this._State0 = state0;
                this._State1 = state1;
            }
        }

        private static void SeedToStates(ulong seed, out uint state0, out uint state1)
        {
            // we treat 'seed' as a composite of the two internal, 32-bit state variables;
            // (State0 * _Modulus1 + State1) is the more compact 64-bit representation,
            // but (State0 << 32 | State1) might be more machine-friendly

            state0 = unchecked((uint)(seed / _Modulus1));
            state1 = unchecked((uint)(seed % _Modulus1));
        }

        private static ulong SeedFromStates(uint state0, uint state1)
        {
            return unchecked(((ulong)state0 * _Modulus1) + (ulong)state1);
        }

        public void Seed32(uint seed)
        {
            if (seed == 0 || seed >= _Modulus)
                seed = 12345;

            unchecked  // disable arithmetic checks for performance
            {
                Seed((ulong)seed * _Modulus1 + 67890);
            }
        }

        private static uint NextFromStates(uint state0, uint state1)
        {
            unchecked  // disable arithmetic checks for performance
            {
                state0 = (uint)(((ulong)state0 * _Multiplier0) % _Modulus);
                state1 = (uint)(((ulong)state1 * _Multiplier1) % _Modulus1);

                int next = (int)(state0 - state1);
                if (next <= 0) next += (int)(_Modulus - 1);

                return (uint)next;
            }
        }

        private static uint NextFromStates(uint state0, uint state1, uint limit)
        {
            unchecked  // disable arithmetic checks for performance
            {
                return (uint)((double)((ulong)NextFromStates(state0, state1) * (ulong)limit) / 2147483589.46728);
            }
        }

        public override ulong Next()
        {
            unchecked  // disable arithmetic checks for performance
            {
                this._State0 = (uint)(((ulong)this._State0 * _Multiplier0) % _Modulus);
                this._State1 = (uint)(((ulong)this._State1 * _Multiplier1) % _Modulus1);

                int next = (int)(this._State0 - this._State1);
                if (next <= 0) next += (int)(_Modulus - 1);

                return (uint)next;
            }
        }

        public override ulong Next(ulong limit)
        {
            //if (limit <= 0 || limit > int.MaxValue)
            //    throw new ArgumentOutOfRangeException("limit must be a positive 32-bit integer");

            unchecked  // disable arithmetic checks for performance
            {
                return (ulong)((double)(this.Next() * limit) / 2147483589.46728);
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
                int current = (int)(this._State0 - this._State1);
                if (current <= 0) current += (int)(_Modulus - 1);

                this._State0 = (uint)(((ulong)this._State0 * _Multiplier0Inverse) % _Modulus);
                this._State1 = (uint)(((ulong)this._State1 * _Multiplier1Inverse) % _Modulus1);

                return (uint)current;
            }
        }

        public override ulong Previous(ulong limit)
        {
            //if (limit <= 0 || limit > int.MaxValue)
            //    throw new ArgumentOutOfRangeException("limit must be a positive 32-bit integer");

            unchecked  // disable arithmetic checks for performance
            {
                return (ulong)((double)(this.Previous() * limit) / 2147483589.46728);
            }
        }

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        private const ulong _MaximumPeriod = ((ulong)_Modulus - 1) * ((ulong)_Modulus1 - 1);

        private static void SeekStateInternal(ref uint state0, ref uint state1, ulong offset, bool reverse)
        {
            if (offset >= _MaximumPeriod)
                offset %= _MaximumPeriod;

            if (offset == 0)
                return;

            {
                MyUInt256 aexpn = new MyUInt256(reverse ? _Multiplier0Inverse : _Multiplier0);
                aexpn.ExpMod(offset, new MyUInt256(_Modulus));

                state0 = unchecked((uint)((aexpn.ToUInt64() * state0) % _Modulus));
            }

            {
                MyUInt256 aexpn = new MyUInt256(reverse ? _Multiplier1Inverse : _Multiplier1);
                aexpn.ExpMod(offset, new MyUInt256(_Modulus1));

                state1 = unchecked((uint)((aexpn.ToUInt64() * state1) % _Modulus1));
            }
        }

        private void SeekInternal(ulong offset, bool reverse)
        {
            SeekStateInternal(ref this._State0, ref this._State1, offset, reverse);
        }

        public override void SeekAhead(ulong offset)
        {
            SeekInternal(offset, false);
        }

        public override void SeekBack(ulong offset)
        {
            SeekInternal(offset, true);
        }

        public override bool CanSeekSeed
        {
            get
            {
                return true;
            }
        }

        public override ulong SeekSeedAhead(ulong seed, ulong offset)
        {
            uint state0, state1;
            SeedToStates(seed, out state0, out state1);

            SeekStateInternal(ref state0, ref state1, offset, false);

            return SeedFromStates(state0, state1);
        }

        public override ulong SeekSeedBack(ulong seed, ulong offset)
        {
            uint state0, state1;
            SeedToStates(seed, out state0, out state1);

            SeekStateInternal(ref state0, ref state1, offset, true);

            return SeedFromStates(state0, state1);
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

            ulong output1 = ((output.Length > 2) ? output[1] : wildcard);

            uint blocksize = (uint)(_Modulus / ((ulong)_Multiplier1 * limit));
            if (blocksize < 2) output1 = wildcard;  // don't attempt improved attack if block size is too small

            RecoverSeedEventArgs args = new RecoverSeedEventArgs();

            unchecked  // disable arithmetic checks for performance
            {
                // operate on output suffix of length N - 1 so we can fix bottom portion of seed to output[0]
                ulong[] suboutput = new ulong[output.Length - 1];
                Array.Copy(output, 1, suboutput, 0, output.Length - 1);

                // 2147483589.46728 rounded down is 2147483589 and rounded up is 2147483590; rounding outward ensures we won't exclude a viable candidate state
                uint difflo = (uint)(((2147483589 * output[0]) + limit - 1) / limit);  // add (limit - 1) to round up, to make sure 'difflo' will (almost always) produce output[0]
                uint diffhi = (uint)(((2147483590 * (output[0] + 1)) + limit - 1) / limit) - 1;  // lowest difference that produces (output[0] + 1), then - 1

                for (uint state0 = 1; state0 < _Modulus; state0++)
                {
                    uint state1start = (state0 + _Modulus - diffhi);  // State1 such that (State0 - State1) == diffhi; subtract 'diffhi' to get State1 lower bound
                    if (state1start >= _Modulus)  // _Modulus rather than _Modulus1 because difference is computed modulo _Modulus
                        state1start -= _Modulus;
                    if (state1start == 0 || state1start >= _Modulus1)
                        state1start = 1;  // skip impossible state1 values

                    if (output1 != wildcard)
                    {
                        uint offset = NextFromStates(state0, state1start, (uint)limit);  // determine how far off current output is from second output
                        offset = offset + ((offset < output1) ? (uint)limit : 0) - (uint)output1;  // compute (offset - output1) because State1 is subtracted from State0

                        if (offset >= 2)  // skip ahead very conservatively to vicinity of a block of consecutive states that will produce 'output1'
                            state1start += (offset - 1) * blocksize - (_Modulus - _Modulus1 + 1);  // don't let skipped states cause us to miss any viable candidates

                        while (NextFromStates(state0, state1start, (uint)limit) != output1)  // TO-DO TODO: binary seek, starting at +'blocksize'
                            state1start++;
                    }

                    uint state1end = (state0 + _Modulus - difflo);  // State1 such that (State0 - State1) == difflo; subtract 'difflo' to get State1 upper bound
                    if (state1end >= _Modulus)
                        state1end -= _Modulus;
                    if (state1end == 0 || state1end >= _Modulus1)
                        state1end = (_Modulus1 - 1);  // stop short of impossible State1 values

                    uint state1blockstart = state1start;
                    uint state1blockend;

                    for (; ; )  // blocks
                    {
                        if (output1 != wildcard)
                        {
                            state1blockend = state1blockstart + blocksize;  // [state1blockstart..state1blockend] is (blocksize + 1) states, to accommodate rounding variations
                            if (state1blockend >= _Modulus)
                                state1blockend -= _Modulus;
                            if (state1blockend == 0 || state1blockend >= _Modulus1)
                                state1blockend = (_Modulus1 - 1);  // stop short of impossible State1 values
                        }
                        else
                        {
                            state1blockstart = state1start;
                            state1blockend = state1end;
                        }

                        for (; ; )  // pieces
                        {
                            // brute-force State1 in two passes if its range wraps around, so that we don't have to check for zero or _Modulus1 after each increment
                            uint state1piecestart = state1blockstart;
                            uint state1pieceend = ((state1blockstart < state1blockend) ? state1blockend : (_Modulus1 - 1));

                            for (uint state1 = state1piecestart; ; state1++)
                            {
                                this._State0 = state0;  // faster than converting states to seed and seed to states via Seed()
                                this._State1 = state1;

                                int i;
                                for (i = 0; i < suboutput.Length; i++)
                                {
                                    if (this.Next(limit) != suboutput[i] && suboutput[i] != wildcard)
                                        break;
                                }

                                if (i == suboutput.Length)  // invoke callback for seed discovery notification
                                {
                                    uint prevstate0 = (uint)(((ulong)state0 * _Multiplier0Inverse) % _Modulus);
                                    uint prevstate1 = (uint)(((ulong)state1 * _Multiplier1Inverse) % _Modulus1);

                                    args.EventType = RecoverSeedEventType.SeedDiscovered;
                                    args.Seed = SeedFromStates(prevstate0, prevstate1);
                                    // TO-DO TODO: estimate progress/total somehow?  would be easy if not for skipped State1 candidates
                                    args.CurrentAttempts = 0;
                                    args.TotalAttempts = 0;

                                    callback(args);

                                    if (args.Cancel)
                                        return false;
                                }

                                if (state1 == state1pieceend)  // check only after body of loop so that 'state1pieceend' will get tested
                                    break;
                            } //for(state1)

                            if (state1blockstart > state1blockend)
                                state1blockstart = 1;  // now brute-force the second (post-zero) piece
                            else break;
                        } //for(pieces)

                        if (output1 != wildcard)
                        {
                            if (state1blockstart <= state1end && state1blockend >= state1end)
                                break;

                            state1blockstart = state1blockend + ((uint)(limit - 1) * blocksize);  // skip ahead by a conservative underestimate
                            if (state1blockend <= state1end && state1blockstart >= state1end)
                                break;
                            // 'state1blockend' will be set at the top of the loop body

                            while (NextFromStates(state0, state1blockstart, (uint)limit) != output1)  // TO-DO TODO: binary seek?
                                state1blockstart++;
                        }
                        else break;
                    } //for(blocks)
                } //for(state0)
            } //unchecked

            return true;
        } //PrngMssql.RecoverSeed

    } //class PrngMssql

} //namespace Cylance.Research.Prangster
