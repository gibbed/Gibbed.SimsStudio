/* Copyright (c) 2011 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.IO;
using Gibbed.Helpers;

namespace Gibbed.TheSimsStudio.AssemblyTool
{
    internal static class Dump
    {
        public static ExitCode Process(string inputPath)
        {
            if (inputPath == null)
            {
                Console.WriteLine("You need to specify an input assembly path.");
                return ExitCode.DecryptError;
            }

            using (var input = File.OpenRead(inputPath))
            {
                var isEncrypted = input.ReadValueB8();
                var typeId = input.ReadValueU32();

                if (isEncrypted == false || (typeId != 0xE3E5B716 && typeId != 0x2BC4F79F))
                {
                    Console.WriteLine("Not an encrypted assembly that I know how to handle.");
                    return ExitCode.DecryptError;
                }

                var theirSum = new byte[64];
                input.Read(theirSum, 0, theirSum.Length);
                var blockCount = input.ReadValueU16();
                var table = new byte[blockCount * 8];
                input.Read(table, 0, table.Length);

                // Calculate initial seed
                uint seed = 0;
                for (int i = 0; i < blockCount; i++)
                {
                    seed += BitConverter.ToUInt32(table, i * 8);
                }
                seed = (uint)(table.Length - 1) & seed;

                for (int i = 0; i < theirSum.Length; i += 16)
                {
                    Console.Write("{0} ", i == 0 ? "Sum ........:" : "             ");
                    for (int j = i; j < theirSum.Length && j < i + 16; j++)
                    {
                        Console.Write("{0:X2}", theirSum[j]);
                    }
                    Console.WriteLine();
                }
                Console.WriteLine("Blocks .....: {0}", blockCount);

                Console.Write("Empty Blocks: ");
                bool notEmpty = false;
                for (int i = 0; i < blockCount; i++)
                {
                    if ((table[(i * 8)] & 1) != 0)
                    {
                        Console.WriteLine("{2}{1:X8} (#{0})",
                            i,
                            i * 512,
                            notEmpty == false ? "" : "              ");
                        notEmpty = true;
                    }
                }
                Console.WriteLine("{0}", notEmpty == true ? "" : "(none)");

                return ExitCode.Success;
            }
        }
    }
}
