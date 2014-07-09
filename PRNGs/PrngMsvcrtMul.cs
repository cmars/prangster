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

    public sealed class PrngMsvcrtMul : PrngMsvcrtBase
    {

        public PrngMsvcrtMul()
            : base()
        {
        }

        public PrngMsvcrtMul(ulong seed)
            : base(seed)
        {
        }

        private static uint NextFromState(uint state, uint limit)
        {
            return unchecked((uint)(((ulong)(PrngMsvcrtBase.NextState(state) >> 16) * limit) / _OutputDivisor));  // & 0x7FFF after >> 16
        }

        public override ulong Next(ulong limit)
        {
            // disable arithmetic checks for performance
            return unchecked(this.Next() * limit / _OutputDivisor);
        }

        public override ulong Previous(ulong limit)
        {
            // disable arithmetic checks for performance
            return unchecked(this.Previous() * limit / _OutputDivisor);
        }

        protected override bool RecoverSeed(ulong seedStart, ulong seedEnd, ulong seedIncrement, ulong[] output, ulong limit, ulong wildcard, RecoverSeedCallback callback, ulong progressInterval)
        {
            if (output.Length < 2 || output[0] > (ulong.MaxValue / _OutputDivisor) || output[0] == wildcard || limit == 0 || limit > _OutputDivisor)
                return base.RecoverSeed(seedStart, seedEnd, seedIncrement, output, limit, wildcard, callback, progressInterval);

            if (seedStart > seedEnd || seedIncrement == 0)
                return false;

            unchecked  // disable arithmetic checks for performance
            {
                // operate on output suffix of length N - 1 so we can fix top portion of seed to output[0]
                ulong[] suboutput = new ulong[output.Length - 1];
                Array.Copy(output, 1, suboutput, 0, output.Length - 1);

                ulong rangestart = ((_OutputDivisor * output[0]) + (uint)limit - 1) / (uint)limit;  // add (limit - 1) to round up, to make sure 'rangestart' will produce output[0]
                rangestart *= _DiscardDivisor;

                ulong rangeend = (((_OutputDivisor * (output[0] + 1)) + (uint)limit - 1) / (uint)limit) - 1;  // lowest seed that produces (output[0] + 1), then - 1
                rangeend = (rangeend * _DiscardDivisor) + (_DiscardDivisor - 1);

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

                ulong output1 = ((output.Length > 2) ? output[1] : wildcard);

                uint blocksize = (uint)(_Modulus / ((ulong)_Multiplier * limit));
                if (blocksize < 2) output1 = wildcard;  // don't attempt improved attack if block size is too small

                if (output1 == wildcard || seedIncrement != 1)
                {
                    this._OriginalCallback = callback;
                    return base.RecoverSeed(rangestart, rangeend, seedIncrement, suboutput, limit, wildcard, this.RecoverSeedCallback, progressInterval);
                }

                // otherwise, use second output to skip blocks of candidate states

                RecoverSeedEventArgs args = new RecoverSeedEventArgs();

                uint offset = NextFromState((uint)rangestart, (uint)limit);  // determine how far off current output is from second output
                offset = (uint)output1 + ((output1 < offset) ? (uint)limit : 0) - offset;

                if (offset >= 2)  // skip ahead conservatively to vicinity of a block of consecutive states that will produce 'output1'
                    rangestart += (offset - 1) * blocksize;

                while (NextFromState((uint)rangestart, (uint)limit) != output1)  // TO-DO TODO: binary seek, starting at +'blocksize'
                    rangestart++;

                uint blockstart = (uint)rangestart;
                uint blockend;

                for (; ; )
                {
                    blockend = blockstart + blocksize;  // [blockstart..blockend] is (blocksize + 1) states, to accommodate rounding variations
                    if (blockend >= rangeend) blockend = (uint)rangeend;

                    for (uint state = blockstart; ; state++)
                    {
                        this.Seed(state);

                        int i;
                        for (i = 0; i < suboutput.Length; i++)
                        {
                            if (this.Next(limit) != suboutput[i] && suboutput[i] != wildcard)
                                break;
                        }

                        if (i == suboutput.Length)  // invoke callback for seed discovery notification
                        {
                            args.EventType = RecoverSeedEventType.SeedDiscovered;
                            args.Seed = PreviousState(state);
                            // TO-DO TODO: estimate progress/total somehow?
                            args.CurrentAttempts = 0;
                            args.TotalAttempts = 0;

                            callback(args);

                            if (args.Cancel)
                                return false;
                        }

                        if (state == blockend)  // check only after body of loop so that 'blockend' will get tested
                            break;
                    } //for(state)

                    blockstart += ((uint)limit * blocksize);
                    if (blockstart >= rangeend)
                        break;
                    // 'blockend' will be set at the top of the loop body

                    while (NextFromState(blockstart, (uint)limit) != output1)  // TO-DO TODO: binary seek?
                        blockstart++;
                } //for(;;)
            } //unchecked

            return true;
        } //PrngMsvcrtMul.RecoverSeed

        private RecoverSeedCallback _OriginalCallback;

        private void RecoverSeedCallback(RecoverSeedEventArgs args)
        {
            if (args.EventType == RecoverSeedEventType.SeedDiscovered)
                args.Seed = PreviousState(unchecked((uint)args.Seed));

            this._OriginalCallback(args);
        }

    } //class PrngMsvcrtMul

} //namespace Cylance.Research.Prangster
