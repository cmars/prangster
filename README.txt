Prangster v1.0.0.0 (last updated 06/28/2013)

Copyright (c) 2013 Cylance, Inc.  All rights reserved.
For updates, please visit <http://www.cylance.com/>.


Prangster is being released in conjunction with the Black Hat USA 2013
presentation "Black-Box Assessment of Pseudorandom Algorithms" by Derek Soeder,
Christopher Abad, and Gabriel Acevedo of Cylance.

Prangster is a utility for recovering information from pseudorandom application
output generated using a pseudorandom number generator (PRNG) it recognizes.
Its current functionality includes recovering an initial state or seed based on
a sample of output (usually many orders of magnitude faster than naive brute-
force), seeking to a future or previous state or seed, and generating the
output that follows or precedes a given state or seed.  For more information,
please consult the presentation slides or white paper or the source code.

Prangster is written in C# and targets the Microsoft .NET Framework 2.0.  To
build it on Windows, copy all of the source files (including the PRNGs and
Properties subdirectories) into a directory, and either open the solution file
(.sln) in Visual Studio 2012, or run the following command from the directory
containing PrangsterApp.cs:

    \Windows\Microsoft.NET\Framework\v2.0.50727\csc.exe /out:Prangster.exe *.cs PRNGs\*.cs Properties\*.cs

Adjust the above path as necessary for your system; for example, the 64-bit
.NET Framework (Framework64) or version 4.0 (v4.0.30319) will also work.

To build Prangster with Mono, open the solution file in MonoDevelop or run the
following command:

    /usr/bin/mcs -out:Prangster.exe *.cs PRNGs/*.cs Properties/*.cs
