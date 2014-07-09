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

    public sealed class PrngMsvcrt : PrngMsvcrtBase
    {

        public PrngMsvcrt()
            : base()
        {
        }

        public PrngMsvcrt(ulong seed)
            : base(seed)
        {
        }

        public override ulong Next(ulong limit)
        {
            unchecked  // disable arithmetic checks for performance
            {
                uint next = (uint)this.Next();

                if (limit != 0 && limit < _OutputDivisor)
                    next %= (uint)limit;

                return next;
            }
        }

        public override ulong Previous(ulong limit)
        {
            unchecked  // disable arithmetic checks for performance
            {
                uint current = (uint)this.Previous();

                if (limit != 0 && limit < _OutputDivisor)
                    current %= (uint)limit;

                return current;
            }
        }

        protected override bool RecoverSeed(ulong seedStart, ulong seedEnd, ulong seedIncrement, ulong[] output, ulong limit, ulong wildcard, RecoverSeedCallback callback, ulong progressInterval)
        {
            if (output.Length < 2 || output[0] >= _OutputDivisor || output[0] == wildcard || seedStart != this.MinimumSeed || seedIncrement != 1 || limit == 0 || limit > _OutputDivisor)
                return base.RecoverSeed(seedStart, seedEnd, seedIncrement, output, limit, wildcard, callback, progressInterval);

            if (seedStart > seedEnd || seedIncrement == 0)
                return false;

            RecoverSeedEventArgs args = new RecoverSeedEventArgs();

            unchecked  // disable arithmetic checks for performance
            {
                // operate on output suffix of length N - 1 so we can fix middle portion of seed to output[0]
                ulong[] suboutput = new ulong[output.Length - 1];
                Array.Copy(output, 1, suboutput, 0, output.Length - 1);

                uint seedtopincr = (uint)limit * _DiscardDivisor;

                // Modulus is a power of 2, so GCD(limit, Modulus) is greatest power-of-two factor of limit
                uint subgenoutputmask = ((uint)limit ^ ((uint)limit - 1)) >> 1;
                uint subgensize = (subgenoutputmask + 1) * _DiscardDivisor;  // GCD(limit, Modulus) * DiscardDivisor

                // fix bottom portion by brute-forcing subgenerator
                uint subseed;
                for (subseed = 0; subseed < subgensize; subseed++)
                {
                    if (subgenoutputmask != 0)  // if limit is odd, we can't check this subgenerator seed against LSB(s) of output; we must test all possibilities below
                    {
                        this.Seed(subseed);

                        int i;
                        for (i = 0; i < suboutput.Length; i++)
                        {
                            // we can do Next() instead of Next(limit) for speed, since either way the remainder modulo power-of-two is preserved
                            if ((this.Next(limit) & subgenoutputmask) != (suboutput[i] & subgenoutputmask) && suboutput[i] != wildcard)
                                break;
                        }

                        if (i < suboutput.Length)
                            continue;
                    }

                    // MSB(s) of subseed will contain LSB(s) of output[0], so | instead of + to keep duplicate bit(s) from interfering
                    for (uint seed = ((uint)output[0] * _DiscardDivisor) | subseed; seed <= seedEnd; seed += seedtopincr)
                    {
                        this.Seed(seed);

                        int i;
                        for (i = 0; i < suboutput.Length; i++)
                        {
                            if (this.Next(limit) != suboutput[i] && suboutput[i] != wildcard)
                                break;
                        }

                        if (i == suboutput.Length)  // invoke callback for seed discovery notification
                        {
                            args.EventType = RecoverSeedEventType.SeedDiscovered;
                            args.Seed = PreviousState(seed);
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
        } //PrngMsvcrt.RecoverSeed

    } //class PrngMsvcrt

} //namespace Cylance.Research.Prangster
