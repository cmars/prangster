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
using System.Collections.Generic;

namespace Cylance.Research.Prangster
{

    public class PrangsterApp
    {

        private static string[] GetPrngNames()
        {
            List<string> prngs = new List<string>();

            foreach (Type prngtype in typeof(PrangsterApp).Assembly.GetTypes())
            {
                if (typeof(PrngBase).IsAssignableFrom(prngtype) && !prngtype.IsAbstract)
                    prngs.Add(prngtype.Name);
            }

            prngs.Sort();
            return prngs.ToArray();
        } //PrangsterApp.GetPrngNames

        private static PrngBase GetPrng(string name)
        {
            try
            {
                Type prngtype = typeof(PrangsterApp).Assembly.GetType(
                    typeof(PrngBase).Namespace + "." + name);

                if (!typeof(PrngBase).IsAssignableFrom(prngtype) || prngtype.IsAbstract)
                    throw new ArgumentException();

                return (PrngBase)Activator.CreateInstance(prngtype, false);
            }
            catch (ArgumentException)
            {
                Console.Error.WriteLine("The specified PRNG does not exist.");
            }

            return null;
        } //PrangsterApp.GetPrng

        public static ulong[] ConvertSymbolsToNumbers(string symbols, string alphabet)
        {
            List<ulong> numbers = new List<ulong>(symbols.Length);

            for (int i = 0; i < symbols.Length; i++)
            {
                char ch = symbols[i];

                int j = alphabet.IndexOf(ch);
                if (j < 0)
                {
                    if (Char.IsWhiteSpace(ch))
                        continue;

                    Console.Error.WriteLine("The input contains a symbol that is not in the alphabet.");
                    return null;
                }

                if ((j + 1) < alphabet.Length && alphabet.IndexOf(ch, (j + 1)) >= 0)
                {
                    Console.Error.WriteLine("Sorry, in this version the alphabet cannot contain duplicates of any symbol.");
                    return null;
                }

                numbers.Add(unchecked((ulong)j));
            }

            return numbers.ToArray();
        } //PrangsterApp.ConvertSymbolsToNumbers

        private static void Usage()
        {
            Console.Error.WriteLine(
@"Usage:  {0} r <prng> <alphabet>
        {0} g <prng> <alphabet> <seed> <length>
        {0} s <prng> <seed> <offset>

Where:     r  recovers seeds that generate the output read from stdin
              (use ""echo xxx|"" or ""<xxx.txt"", or type and press Ctrl+Z to end)

           g  if <length> is positive, reproduces the next <length> output
              symbol(s) generated after initiailziing to state <seed>;
              if <length> is negative, reproduces in forward order the previous
              -<length> output symbol(s) generated before reaching state <seed>

           s  seeks from <seed> by <offset> state(s) and displays the seed
              representing the new state; a negative <offset> seeks backward

      <prng>  is one of the following PRNGs:
                {1}

  <alphabet>  is a string that maps the symbols comprising the application's
              pseudorandom output to numbers, based on a given symbol's
              position in the string or the symbol at a given position",
                typeof(PrangsterApp).Assembly.ManifestModule.Name,
                String.Join("\r\n                ", GetPrngNames()));
        } //PrangsterApp.Usage

        public static void Main(string[] args)
        {
            if (args.Length < 1 || String.IsNullOrEmpty(args[0]))
            {
                Usage();
                return;
            }

            switch (char.ToLowerInvariant(args[0][0]))
            {
                case 'r':
                    {
                        if (args.Length != 3 || String.IsNullOrEmpty(args[1]) || String.IsNullOrEmpty(args[2]) || !args[1].StartsWith("Prng"))
                        {
                            Usage();
                            return;
                        }

                        PrngBase prng = GetPrng(args[1]);

                        if (prng == null)
                            return;

                        string outputsymbols = Console.In.ReadToEnd();

                        if (outputsymbols == null)
                        {
                            Console.Error.WriteLine("No pseudorandom output sample could be read.");
                            return;
                        }

                        ulong[] outputnumbers = ConvertSymbolsToNumbers(outputsymbols, args[2]);

                        if (outputnumbers == null)
                            return;

                        if (outputnumbers.Length < 1)
                        {
                            Console.Error.WriteLine("The pseudorandom output sample was too short.");
                            return;
                        }

                        prng.RecoverSeed(
                            outputnumbers,
                            (ulong)args[2].Length, ulong.MaxValue,
                            delegate(RecoverSeedEventArgs e)
                            {
                                if (e.EventType == RecoverSeedEventType.SeedDiscovered)
                                    Console.WriteLine(e.Seed);
                                else Console.Error.Write('.');
                            },
                            1000000);
                    }
                    break; //case 'r'

                case 'g':
                    {
                        ulong seed;
                        int length;

                        if (args.Length != 5 || String.IsNullOrEmpty(args[1]) || String.IsNullOrEmpty(args[2]) || !args[1].StartsWith("Prng") ||
                            !ulong.TryParse(args[3], out seed) || !int.TryParse(args[4], out length) || length == 0 || (length < 0 && unchecked(-length) < 0))
                        {
                            Usage();
                            return;
                        }

                        PrngBase prng = GetPrng(args[1]);

                        if (prng == null)
                            return;

                        prng.Seed(seed);

                        string alphabet = args[2];
                        uint alphabetsize = (uint)alphabet.Length;

                        char[] output;

                        if (length >= 0)
                        {
                            output = new char[length];

                            for (int i = 0; i < length; i++)
                                output[i] = alphabet[unchecked((int)prng.Next(alphabetsize))];
                        }
                        else
                        {
                            if (!prng.CanReverse)
                            {
                                Console.Error.WriteLine("This PRNG does not support reversing.");
                                return;
                            }

                            length = -length;

                            output = new char[length];

                            for (int i = length; i != 0; )
                                output[--i] = alphabet[unchecked((int)prng.Previous(alphabetsize))];
                        }

                        Console.WriteLine(output);
                    }
                    break; //case 'g'

                case 's':
                    {
                        ulong seed;
                        long offset;

                        if (args.Length != 4 || String.IsNullOrEmpty(args[1]) || !args[1].StartsWith("Prng") ||
                            !ulong.TryParse(args[2], out seed) || !long.TryParse(args[3], out offset))
                        {
                            Usage();
                            return;
                        }

                        PrngBase prng = GetPrng(args[1]);

                        if (prng == null)
                            return;

                        if (!prng.CanSeekSeed)
                        {
                            Console.Error.WriteLine("This PRNG does not support seed seeking.");
                            return;
                        }

                        prng.Seed(seed);

                        if (offset >= 0)
                            seed = prng.SeekSeedAhead(seed, (ulong)offset);
                        else seed = prng.SeekSeedBack(seed, unchecked((ulong)-offset));

                        Console.WriteLine(seed);
                    }
                    break; //case 's'

                default:
                    Usage();
                    return;
            } //switch(args[0][0])
        } //PrangsterApp.Main

    } //class PrangsterApp

} //namespace Cylance.Research.Prangster
