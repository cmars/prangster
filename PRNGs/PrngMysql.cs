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

// Based on MySQL sql/item_func.cc and mysys_ssl/my_rnd.cc, which bear the following copyright notice and license:
/*
   Copyright (c) 2000, 2012, Oracle and/or its affiliates. All rights reserved.

   This program is free software; you can redistribute it and/or modify
   it under the terms of the GNU General Public License as published by
   the Free Software Foundation; version 2 of the License.

   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU General Public License for more details.

   You should have received a copy of the GNU General Public License
   along with this program; if not, write to the Free Software
   Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA */

using System;

namespace Cylance.Research.Prangster
{

    public sealed class PrngMysql : PrngBase
    {

        private const uint _Modulus = 0x3FFFFFFF;  // = 3 * 3 * 7 * 11 * 31 * 151 * 331

        private uint _Seed1;
        private uint _Seed2;

        public PrngMysql()
        {
        }

        public PrngMysql(ulong seed)
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
                // _Modulus is divisible by 3, and Seed1 gets multiplied by 3 immediately,
                // so only Seed1 % (_Modulus / 3) matters; we restrict the seed space accordingly
                return ((ulong)_Modulus / 3 * (ulong)_Modulus) - 1;
            }
        }

        public void Seed32(uint seed)  // Item_func_rand::seed_random
        {
            unchecked  // disable arithmetic checks for performance
            {
                this._Seed1 = (seed * 0x10001 + 55555555) % _Modulus;
                this._Seed2 = (seed * 0x10000001) % _Modulus;
            }
        }

        public override void Seed(ulong seed)  // randominit
        {
            // we treat 'seed' as a composite of the two internal, 32-bit state variables;
            // (Seed1 * _Modulus + Seed2) is the more compact 64-bit representation,
            // but (Seed1 << 32 | Seed2) might be more machine-friendly

            //if (seed < 0 || seed > ((ulong)_Modulus / 3 * (ulong)_Modulus) - 1)
            //    throw new ArgumentOutOfRangeException("seed must comprise two integers each less than modulus");

            unchecked  // disable arithmetic checks for performance
            {
                this._Seed1 = unchecked((uint)(seed / _Modulus));
                this._Seed2 = unchecked((uint)(seed % _Modulus));
            }
        }

        public override ulong Next()  // my_rnd
        {
            unchecked  // disable arithmetic checks for performance
            {
                // arithmetically safe, because (max * 3) + max = 0x3FFFFFFE * 4 = 0xFFFFFFF8
                uint next = ((this._Seed1 * 3) + this._Seed2) % _Modulus;

                this._Seed1 = next;
                this._Seed2 = (this._Seed2 + next + 33) % _Modulus;

                return next;
            }
        }

        public override ulong Next(ulong limit)
        {
            //if (limit <= 0 || limit > int.MaxValue)
            //    throw new ArgumentOutOfRangeException("limit must be a positive 32-bit integer");

            unchecked  // disable arithmetic checks for performance
            {
                return (this.Next() * limit) / _Modulus;
            }
        }

        public override bool CanReverse
        {
            get
            {
                // we can't reverse because * 3 and modulo destroy most-significant part of Seed1 (because _Modulus is divisible by 3),
                // and because we need that lost information to return correct take-from-top results
                return false;
            }
        }

#if false
        public override ulong Previous()
        {
            uint current = this._Seed1;

            unchecked  // disable arithmetic checks for performance
            {
                int prevseed2 = (int)(this._Seed2 - this._Seed1 - 33);
                while (prevseed2 < 0) prevseed2 += (int)_Modulus;

                int prevseed1 = (int)(this._Seed1 - prevseed2);
                if (prevseed1 < 0) prevseed1 += (int)_Modulus;

                if ((prevseed1 % 3) != 0)
                    throw new InvalidOperationException("intermediate previous Seed1 must always be divisible by 3");

                this._Seed1 = (uint)(prevseed1 / 3);
                this._Seed2 = (uint)prevseed2;

                return current;
            }
        }

        public override ulong Previous(ulong limit)
        {
            //if (limit <= 0 || limit > int.MaxValue)
            //    throw new ArgumentOutOfRangeException("limit must be a positive 32-bit integer");

            unchecked  // disable arithmetic checks for performance
            {
                return (this.Previous() * limit) / _Modulus;
            }
        }
#endif

    } //class PrngMysql

} //namespace Cylance.Research.Prangster
