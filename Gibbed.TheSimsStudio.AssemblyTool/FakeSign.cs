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
using System.Text;
using Gibbed.Helpers;
using Mono.Cecil;

namespace Gibbed.TheSimsStudio.AssemblyTool
{
    internal static class FakeSign
    {
        public static ExitCode Process(string inputPath, string outputPath, bool stripTargetFramework)
        {
            if (inputPath == null)
            {
                Console.WriteLine("You need to specify an input assembly path.");
                return ExitCode.DecryptError;
            }

            if (outputPath == null)
            {
                outputPath = Path.ChangeExtension(inputPath, null);
                if (Path.GetExtension(outputPath) != ".dll")
                {
                    outputPath += ".dll";
                }
            }

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

            var blockCount = data.Length.Align(512) / 512;
            if (blockCount > ushort.MaxValue)
            {
                Console.WriteLine("File is too large to fit in a signed assembly.");
                return ExitCode.FakeSignError;
            }

            var sum = new byte[64];
            var sumMessage = Encoding.ASCII.GetBytes("This is a fake signed assembly.");
            Array.Copy(sumMessage, sum, sumMessage.Length);

            var table = new byte[blockCount * 8];

            // Calculate initial seed
            uint seed = 0;
            for (int i = 0; i < blockCount; i++)
            {
                //table[i * 8] = 1;
                seed += BitConverter.ToUInt32(table, i * 8);
            }
            seed = (uint)(table.Length - 1) & seed;

            using (var output = File.Open(outputPath, FileMode.Create, FileAccess.Write))
            {
                output.WriteValueB8(true);
                output.WriteValueU32(0xE3E5B716);
                output.Write(sum, 0, sum.Length);
                output.WriteValueU16((ushort)blockCount);
                output.Write(table, 0, table.Length);

                // Encrypt data
                for (int i = 0; i < blockCount; i++)
                {
                    byte[] block = new byte[512];
                    data.Read(block, 0, block.Length);

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
