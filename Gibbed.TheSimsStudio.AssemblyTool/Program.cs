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
using System.Collections.Generic;
using System.IO;
using NDesk.Options;

namespace Gibbed.TheSimsStudio.AssemblyTool
{
    internal class Program
    {
        public static int Main(string[] args)
        {
            var showHelp = false;
            var mode = Mode.Unknown;
            string inputPath = null;
            string outputPath = null;
            string signaturePath = null;
            bool stripTargetFramework = true;

            var options = new OptionSet()
            {
                {
                    "e|encrypt",
                    "Encrypt assembly",
                    v => mode = v == null ? mode : Mode.Encrypt
                },
                {
                    "d|decrypt",
                    "Decrypt assembly",
                    v => mode = v == null ? mode : Mode.Decrypt
                },
                {
                    "b|borrow",
                    "Encrypt assembly using an existing assembly",
                    v => mode = v == null ? mode : Mode.Borrow
                },
                {
                    "f|fakesign",
                    "Fakesign assembly",
                    v => mode = v == null ? mode : Mode.Borrow
                },
                {
                    "x|dump",
                    "Dump encrypted assembly information",
                    v => mode = v == null ? mode : Mode.Dump
                },
                {
                    "i|input=",
                    "Input assembly file path",
                    v => inputPath = v
                },
                {
                    "o|output=",
                    "Output assembly file path",
                    v => outputPath = v
                },
                {
                    "s|signature=",
                    "Assembly file path that will be used for its signature",
                    v => signaturePath = v
                },
                {
                    "no-strip=",
                    "Don't strip TargetFrameworkAttribute with Cecil",
                    v => stripTargetFramework = v == null
                },
                {
                    "h|help",
                    "show this message and exit", 
                    v => showHelp = v != null
                },
            };

            List<string> extras;

            try
            {
                extras = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", GetExecutableName());
                return (int)ExitCode.UnknownError;
            }

            if (extras.Count != 0 || showHelp == true || mode == Mode.Unknown)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return (int)ExitCode.Help;
            }

            if (mode == Mode.Decrypt)
            {
                return (int)Decrypt.Process(inputPath, outputPath);
            }
            else if (mode == Mode.Encrypt)
            {
                Console.WriteLine("Sorry, not implemented.");
                return (int)ExitCode.EncryptError;
            }
            else if (mode == Mode.Borrow)
            {
                return (int)Borrow.Process(inputPath, outputPath, signaturePath, stripTargetFramework);
            }
            else if (mode == Mode.FakeSign)
            {
                return (int)FakeSign.Process(inputPath, outputPath, stripTargetFramework);
            }
            else if (mode == Mode.Dump)
            {
                return (int)Dump.Process(inputPath);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }
    }
}
