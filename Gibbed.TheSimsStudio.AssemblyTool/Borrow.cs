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
using System.Linq;
using Gibbed.Helpers;
using Mono.Cecil;

namespace Gibbed.TheSimsStudio.AssemblyTool
{
    internal static class Borrow
    {
        public static ExitCode Process(
            string inputPath, string outputPath, string signaturePath, bool stripTargetFramework)
        {
            if (inputPath == null)
            {
                Console.WriteLine("You need to specify an input assembly path.");
                return ExitCode.DecryptError;
            }
            else if (signaturePath == null)
            {
                Console.WriteLine("You need to specify an assembly path that the signature will be borrowed from.");
                return ExitCode.DecryptError;
            }

            if (outputPath == null)
            {
                outputPath = inputPath += ".s3sa";
            }

            byte[] theirSum;
            ushort blockCount;
            byte[] table;

            using (var original = File.OpenRead(signaturePath))
            {
                var isEncrypted = original.ReadValueB8();
                var typeId = original.ReadValueU32();

                if (isEncrypted == false || (typeId != 0xE3E5B716 && typeId != 0x2BC4F79F))
                {
                    Console.WriteLine("Not an encrypted assembly that I know how to handle.");
                    return ExitCode.BorrowError;
                }

                theirSum = new byte[64];
                original.Read(theirSum, 0, theirSum.Length);
                blockCount = original.ReadValueU16();
                table = new byte[blockCount * 8];
                original.Read(table, 0, table.Length);
            }

            // Calculate initial seed
            uint seed = 0;
            for (int i = 0; i < blockCount; i++)
            {
                seed += BitConverter.ToUInt32(table, i * 8);
            }
            seed = (uint)(table.Length - 1) & seed;

            MemoryStream data;
            using (var input = File.OpenRead(inputPath))
            {
                data = input.ReadToMemoryStream(input.Length);
            }

            if (stripTargetFramework == true)
            {
                var assembly = AssemblyDefinition.ReadAssembly(data);
                var targetFrameworks = assembly.CustomAttributes
                    .Where(c => c.AttributeType.Name == "TargetFrameworkAttribute").ToArray();
                if (targetFrameworks.Length > 0)
                {
                    foreach (var targetFramework in targetFrameworks)
                    {
                        assembly.CustomAttributes.Remove(targetFramework);
                    }

                    data.SetLength(0);
                    assembly.Write(data);
                    data.Position = 0;
                }
            }

            var inputBlocks = data.Length.Align(512) / 512;

            if (inputBlocks > blockCount)
            {
                Console.WriteLine(
                    "Signature assembly must have at least the same number of " +
                    "blocks as input assembly must have the same block count! " +
                    "({0} < {1})",
                    blockCount, inputBlocks);
                return ExitCode.BorrowError;
            }

            data.Seek(0, SeekOrigin.Begin);
            for (int i = 0; i < blockCount; i++)
            {
                var block = new byte[512];

                if (data.Position < data.Length)
                {
                    data.Read(block, 0, block.Length);
                }

                if ((table[(i * 8)] & 1) != 0) // validate this is an empty block
                {
                    for (int j = 0; j < block.Length; j++)
                    {
                        if (block[j] != 0)
                        {
                            Console.WriteLine("Block {0} should have been empty, but wasn't!", i);
                            return ExitCode.BorrowError;
                        }
                    }
                }
            }

            data.Seek(0, SeekOrigin.Begin);
            using (var output = File.Create(outputPath))
            {
                output.WriteValueB8(true);
                output.WriteValueU32(0xE3E5B716);
                output.Write(theirSum, 0, theirSum.Length);
                output.WriteValueU16(blockCount);
                output.Write(table, 0, table.Length);

                // Encrypt data
                for (int i = 0; i < blockCount; i++)
                {
                    byte[] block = new byte[512];

                    if (data.Position < data.Length)
                    {
                        data.Read(block, 0, block.Length);
                    }

                    if ((table[(i * 8)] & 1) == 0) // non-empty block
                    {
                        for (int j = 0; j < 512; j++)
                        {
                            block[j] ^= table[seed];
                            seed = (uint)((seed + block[j]) % table.Length);
                        }

                        output.Write(block, 0, block.Length);
                    }
                }

                return ExitCode.Success;
            }
        }
    }
}
