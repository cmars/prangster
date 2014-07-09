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
    /// Base class from which Lehmer or Park-Miller PRNGs are derived.
    /// </summary>
    public abstract class PrngLehmerBase : PrngLcgBase
    {

        public override ulong MinimumSeed
        {
            get
            {
                return 1;  // seed of 0 is unsupported, because it would cause the internal state to remain at 0 due to the lack of an addition (increment)
            }
        }

        public override ulong Increment
        {
            get
            {
                return 0;  // Lehmer PRNG (multiplicative congruential method) differs from mixed congruential method in its lack of an addition.
            }
        }

        /// <summary>
        /// Computes the next state of the linear congruential generator.
        /// </summary>
        /// <param name="state">The current state, or seed, of the linear congruential generator.</param>
        /// <param name="multiplier">The linear congruential generator's multiplier parameter.</param>
        /// <param name="divisor">The linear congruential generator's &quot;modulus&quot; parameter, or 0 if the modulus is 2**64.</param>
        /// <returns>The next state, modulo <paramref name="divisor"/>.</returns>
        protected static ulong NextState(ulong state, ulong multiplier, ulong divisor)
        {
            return NextState(state, multiplier, 0, divisor);
        }

        /// <summary>
        /// Computes the previous state of the linear congruential generator.
        /// </summary>
        /// <param name="state">The current state, or seed, of the linear congruential generator.</param>
        /// <param name="multiplierInverse">The multiplicative inverse of the linear congruential generator's multiplier parameter modulo <paramref name="divisor"/>.</param>
        /// <param name="divisor">The linear congruential generator's &quot;modulus&quot; parameter, or 0 if the modulus is 2**64.</param>
        /// <returns>The next state, modulo <paramref name="divisor"/>.</returns>
        protected static ulong PreviousState(ulong state, ulong multiplier, ulong divisor)
        {
            return PreviousState(state, multiplier, divisor);
        }

        /// <summary>
        /// Computes the state at the specified position in the pseudorandom stream relative to a given initial state.
        /// </summary>
        /// <param name="state">The state at position zero in the stream.</param>
        /// <param name="offset">The number of states to skip.</param>
        /// <param name="multiplier">The linear congruential generator's multiplier constant, or its multiplicative inverse modulo <paramref name="divisor"/> to go backwards.</param>
        /// <param name="divisor">The linear congruential generator's modulus constant.</param>
        /// <returns>The state at position <paramref name="offset"/> in the stream.</returns>
        protected static ulong SeekState(ulong state, ulong offset, ulong multiplier, ulong divisor)
        {
            // NewState = (Multiplier**Offset * OldState)
            // caller can substitute Multiplier**-1 to go backwards

            if (offset == 0)
                return state;

            MyUInt256 fullmodulus = (divisor == 0 ? new MyUInt256(0, 1) : new MyUInt256(divisor));

            MyUInt256 aexpn = new MyUInt256(multiplier);
            aexpn.ExpMod(offset, fullmodulus);
            aexpn.Multiply(state);

            if (divisor != 0)
                aexpn.Modulo(fullmodulus);

            return aexpn.ToUInt64();
        } //PrngLehmerBase.SeekState

        public override void SeekAhead(ulong offset)
        {
            this._LcgState = SeekState(this._LcgState, offset, this.Multiplier, this.Modulus);
        } //PrngLehmerBase.SeekAhead

        public override void SeekBack(ulong offset)
        {
            if (offset == 0)
                return;

            ulong multiplier = this.Multiplier;
            ulong modulus = this.Modulus;

            ulong mi = GetMultiplicativeInverse(multiplier, modulus);

            if (mi == 0)
            {
                // guessing that (modulus - 1) is a multiple of period (-1 because zero state is excluded),
                // in which case seeking ahead (modulus - 1 - offset) states will reverse by 'offset' states
                SeekAhead(unchecked(modulus - 1 - offset));
                return;
            }

            this._LcgState = SeekState(this._LcgState, offset, mi, modulus);
        } //PrngLehmerBase.SeekBack

        public override ulong SeekSeedAhead(ulong seed, ulong offset)
        {
            return SeekState(this._LcgState, offset, this.Multiplier, this.Modulus);
        } //PrngLehmerBase.SeekSeedAhead

        public override ulong SeekSeedBack(ulong seed, ulong offset)
        {
            if (offset == 0)
                return seed;

            ulong multiplier = this.Multiplier;
            ulong modulus = this.Modulus;

            ulong mi = GetMultiplicativeInverse(multiplier, modulus);

            if (mi == 0)
            {
                // guessing that (modulus - 1) is a multiple of period (-1 because zero state is excluded),
                // in which case seeking ahead (modulus - 1 - offset) states will reverse by 'offset' states
                return SeekSeedAhead(seed, unchecked(modulus - 1 - offset));
            }

            return SeekState(this._LcgState, offset, mi, modulus);
        } //PrngLehmerBase.SeekSeedBack

    } //class PrngLehmerBase

} //namespace Cylance.Research.Prangster
