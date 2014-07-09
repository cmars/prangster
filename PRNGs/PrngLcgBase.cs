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
    /// Base class from which linear congruential generator (LCG)-based PRNGs are derived.
    /// </summary>
    public abstract class PrngLcgBase : PrngBase
    {

        /// <summary>
        /// Default 64-bit internal state.
        /// </summary>
        protected ulong _LcgState;

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
                return (this.Modulus - 1);
            }
        }

        public override void Seed(ulong seed)
        {
            this._LcgState = (seed % this.Modulus);
        }

        /// <summary>
        /// Computes the next state of the linear congruential generator.
        /// </summary>
        /// <param name="state">The current state, or seed, of the linear congruential generator.</param>
        /// <param name="multiplier">The linear congruential generator's multiplier parameter.</param>
        /// <param name="increment">The linear congruential generator's increment or addend parameter.</param>
        /// <param name="divisor">The linear congruential generator's &quot;modulus&quot; parameter, or 0 if the modulus is 2**64.</param>
        /// <returns>The next state, modulo <paramref name="divisor"/>.</returns>
        protected static ulong NextState(ulong state, ulong multiplier, ulong increment, ulong divisor)
        {
            ulong next = ((state * multiplier) + increment);
            if (divisor != 0)
                next %= divisor;

            return next;
        }

        /// <summary>
        /// Performs discard and modulus transformations on a raw value to produce output according to the PRNG's parameters.
        /// </summary>
        /// <param name="raw">The raw bits to transform.</param>
        /// <returns>A value suitable for output.</returns>
        protected virtual ulong ValueFromRaw(ulong raw)
        {
            if (this.DiscardDivisor != 0)
                raw /= this.DiscardDivisor;

            if (this.OutputDivisor != 0)
                raw %= this.OutputDivisor;

            return raw;
        }

        /// <summary>
        /// Performs discard, modulus, and/or take-from-top transformations on a raw value to produce output according to the PRNG's parameters.
        /// </summary>
        /// <param name="raw">The raw bits to transform.</param>
        /// <param name="limit">The lowest positive integer greater than the maximum desired value.</param>
        /// <returns>A value suitable for output.</returns>
        protected virtual ulong ValueFromRaw(ulong raw, ulong limit)
        {
            if (this.DiscardDivisor != 0)
            {
                raw /= this.DiscardDivisor;

                if (this.OutputDivisor != 0)
                    raw %= this.OutputDivisor;

                return raw;
            }
            else
            {
                // TO-DO TODO: should we incorporate OutputDivisor somehow?
                MyUInt256 product = new MyUInt256(raw);
                product.Multiply(limit);
                product.Divide(this.Modulus);

                return (product.ToUInt64());
            }
        }

        public override ulong Next()
        {
            ulong next = NextState(this._LcgState, this.Multiplier, this.Increment, this.Modulus);
            this._LcgState = next;

            return ValueFromRaw(next);
        }

        public override ulong Next(ulong limit)
        {
            ulong next = NextState(this._LcgState, this.Multiplier, this.Increment, this.Modulus);
            this._LcgState = next;

            return ValueFromRaw(next, limit);
        }

        public override bool CanReverse
        {
            get
            {
                return true;
            }
        }

        protected static ulong GetMultiplicativeInverse(ulong multiplier, ulong divisor)
        {
            ulong mi;

            if (multiplier >= divisor && divisor != 0)
                return 0;

            if (unchecked((long)multiplier) < 0 || unchecked((long)divisor) <= 0)
            {
                mi = ModularMath.MultiplicativeInverse(unchecked((long)multiplier), unchecked((long)divisor));
            }
            else
            {
                MyUInt256 mibig = ModularMath.MultiplicativeInverse(new MyUInt256(multiplier), (divisor == 0 ? new MyUInt256(0, 1) : new MyUInt256(divisor)));
                mi = (mibig == null ? 0 : mibig.ToUInt64());
            }

            return mi;
        } //PrngLcgBase.GetMultiplicativeInverse

        /// <summary>
        /// Computes the previous state of the linear congruential generator.
        /// </summary>
        /// <param name="state">The current state, or seed, of the linear congruential generator.</param>
        /// <param name="multiplierInverse">The multiplicative inverse of the linear congruential generator's multiplier parameter modulo <paramref name="divisor"/>.</param>
        /// <param name="increment">The linear congruential generator's increment or addend parameter.</param>
        /// <param name="divisor">The linear congruential generator's &quot;modulus&quot; parameter, or 0 if the modulus is 2**64.</param>
        /// <returns>The next state, modulo <paramref name="divisor"/>.</returns>
        protected static ulong PreviousState(ulong state, ulong multiplier, ulong increment, ulong divisor)
        {
            ulong mi = GetMultiplicativeInverse(multiplier, divisor);

            if (mi != 0)
            {
                unchecked
                {
                    ulong prev = ((state - increment) * mi);
                    if (divisor != 0)
                        prev %= divisor;
                    return prev;
                }
            }
            else
            {
                // guessing that modulus will be a multiple of period, in which case seeking ahead (modulus - 1) states will reverse to previous state
                return SeekState(state, unchecked(divisor - 1), multiplier, increment, divisor);
            }
        } //PrngLcgBase.PreviousState

        public override ulong Previous()
        {
            ulong current = this._LcgState;

            SeekBack(1);
            return ValueFromRaw(current);
        }

        public override ulong Previous(ulong limit)
        {
            ulong current = this._LcgState;

            SeekBack(1);
            return ValueFromRaw(current, limit);
        }

        /// <summary>
        /// When overwritten in a derived class, gets the multiplier constant of the linear congruential generator.
        /// </summary>
        public abstract ulong Multiplier
        {
            get;
        }

        /// <summary>
        /// When overwritten in a derived class, gets the increment (addend) constant of the linear congruential generator.
        /// </summary>
        public abstract ulong Increment
        {
            get;
        }

        /// <summary>
        /// When overwritten in a derived class, gets the modulus constant of the linear
        /// congruential generator, or 0 if the modulus is 2**64.
        /// </summary>
        public abstract ulong Modulus
        {
            get;
        }

        /// <summary>
        /// When overwritten in a derived class, gets the divisor used to divide the
        /// linear congruential generator's output to discard the least-significant portion,
        /// or 0 if the most-significant portion if the output is used instead.
        /// </summary>
        public abstract ulong DiscardDivisor
        {
            get;
        }

        /// <summary>
        /// When overwritten in a derived class, gets the divisor used to retain a remainder
        /// of the linear congruential generator's output after division by
        /// <see cref="DiscardDivisor"/>, or 0 if no modulo operation is to be performed.
        /// </summary>
        public virtual ulong OutputDivisor
        {
            get
            {
                return 0;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Computes the state at the specified position in the pseudorandom stream relative to a given initial state.
        /// </summary>
        /// <param name="state">The state at position zero in the stream.</param>
        /// <param name="offset">The number of states to skip.</param>
        /// <param name="multiplier">The linear congruential generator's multiplier constant, or its multiplicative inverse modulo <paramref name="divisor"/> to go backwards.</param>
        /// <param name="increment">The linear congruential generator's increment constant,
        /// or its additive inverse modulo <paramref name="divisor"/> multiplied by the multiplicative inverse to go backwards.</param>
        /// <param name="divisor">The linear congruential generator's modulus constant.</param>
        /// <returns>The state at position <paramref name="offset"/> in the stream.</returns>
        protected static ulong SeekState(ulong state, ulong offset, ulong multiplier, ulong increment, ulong divisor)
        {
            // NewState = (Multiplier**Offset * OldState) + Increment * (Multiplier**Offset - 1) / (Multiplier - 1)
            // caller can substitute Multiplier**-1 and -Increment * Multiplier**-1 to go backwards

            if (offset == 0)
                return state;

            if (multiplier == 0)
            {
                return increment;
            }
            else if (multiplier == 1)
            {
                MyUInt256 product = new MyUInt256(increment);
                product.Multiply(offset);
                if (divisor != 0)
                    product.Modulo(divisor);
                return product.ToUInt64();
            }

            MyUInt256 compdivisor = new MyUInt256(divisor);
            if (divisor == 0)
                compdivisor.Set(0, 1);
            compdivisor.Multiply(multiplier - 1);

            MyUInt256 aexpn = new MyUInt256(multiplier);
            aexpn.ExpMod(offset, compdivisor);  // = Multiplier**Offset

            MyUInt256 fraction = new MyUInt256(aexpn);
            fraction.Subtract(1);
            fraction.Divide(multiplier - 1);  // should always divide evenly; that's why divisor has to include (Multiplier - 1) factor though
            fraction.Multiply(increment);  // = Increment * (Multiplier**Offset - 1) / (Multiplier - 1)

            MyUInt256 newstate = new MyUInt256(state);
            newstate.Multiply(aexpn);  // = Multiplier**Offset * OldState
            newstate.Add(fraction);  // = (Multiplier**Offset * OldState) + Increment * (Multiplier**Offset - 1) / (Multiplier - 1)

            if (divisor != 0)
                newstate.Modulo(divisor);  // now that we've divided by (Multiplier - 1), we can go back to using 'divisor' instead of 'compdivisor'

            return newstate.ToUInt64();
        } //PrngLcgBase.SeekState

        public override void SeekAhead(ulong offset)
        {
            this._LcgState = SeekState(this._LcgState, offset, this.Multiplier, this.Increment, this.Modulus);
        }

        public override void SeekBack(ulong offset)
        {
            if (offset == 0)
                return;

            ulong multiplier = this.Multiplier;
            ulong increment = this.Increment;
            ulong divisor = this.Modulus;

            ulong mi = GetMultiplicativeInverse(multiplier, divisor);

            if (mi == 0)
            {
                // guessing that modulus is a multiple of period, in which case seeking ahead (modulus - offset) states will reverse by 'offset' states
                SeekAhead(unchecked(divisor - offset));
                return;
            }

            if (increment >= divisor && divisor != 0)
                increment %= divisor;

            if (increment != 0)
            {
                MyUInt256 incrinverse = new MyUInt256(unchecked(divisor - increment));  // negate increment (modulo divisor) to get additive inverse
                incrinverse.Multiply(mi);

                if (divisor != 0)
                    incrinverse.Modulo(divisor);

                increment = incrinverse.ToUInt64();
            }

            this._LcgState = SeekState(this._LcgState, offset, mi, increment, divisor);
        } //PrngLcgBase.SeekBack

        public override bool CanSeekSeed
        {
            get
            {
                return true;
            }
        }

        public override ulong SeekSeedAhead(ulong seed, ulong offset)
        {
            return SeekState(seed, offset, this.Multiplier, this.Increment, this.Modulus);
        } //PrngLcgBase.SeekSeedAhead

        public override ulong SeekSeedBack(ulong seed, ulong offset)
        {
            if (offset == 0)
                return seed;

            ulong multiplier = this.Multiplier;
            ulong increment = this.Increment;
            ulong divisor = this.Modulus;

            ulong mi = GetMultiplicativeInverse(multiplier, divisor);

            if (mi == 0)
            {
                // guessing that modulus is a multiple of period, in which case seeking ahead (modulus - offset) states will reverse by 'offset' states
                return SeekSeedAhead(seed, unchecked(divisor - offset));
            }

            if (increment >= divisor && divisor != 0)
                increment %= divisor;

            if (increment != 0)
            {
                MyUInt256 incrinverse = new MyUInt256(unchecked(divisor - increment));  // negate increment (modulo divisor) to get additive inverse
                incrinverse.Multiply(mi);

                if (divisor != 0)
                    incrinverse.Modulo(divisor);

                increment = incrinverse.ToUInt64();
            }

            return SeekState(this._LcgState, offset, mi, increment, divisor);
        } //PrngLcgBase.SeekSeedBack

    } //class PrngLcgBase

} //namespace Cylance.Research.Prangster
