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
using System.Xml.XPath;
using Gibbed.TheSimsStudio.FileFormats;
using NDesk.Options;

namespace Gibbed.TheSimsStudio.Pack
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            bool showHelp = false;

            OptionSet options = new OptionSet()
            {
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
                return;
            }

            if (extras.Count < 1 || extras.Count > 2 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input [output]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string filesPath = extras[0];
            string filesBasePath;
            
            if (Directory.Exists(filesPath) == true)
            {
                filesBasePath = filesPath;
                filesPath = Path.Combine(filesBasePath, "files.xml");
            }
            else
            {
                filesBasePath = Path.GetDirectoryName(filesPath);
            }

            string outputPath = extras.Count > 1 ?
                extras[1] : Path.ChangeExtension(filesBasePath, ".package");

            var document = new XPathDocument(filesPath);
            var navigator = document.CreateNavigator();
            var nodes = navigator.Select("/files/file");

            using (var output = File.Create(outputPath))
            {
                var db = new Database(output, false);
                while (nodes.MoveNext())
                {
                    string groupText = nodes.Current.GetAttribute("groupid", "");
                    string instanceText = nodes.Current.GetAttribute("instanceid", "");
                    string typeText = nodes.Current.GetAttribute("typeid", "");

                    if (groupText == null || instanceText == null || typeText == null)
                    {
                        throw new InvalidDataException("file missing attributes");
                    }

                    ResourceKey key;
                    if (Hex.TryParseU32(groupText, out key.GroupId) == false ||
                        Hex.TryParseU64(instanceText, out key.InstanceId) == false ||
                        Hex.TryParseU32(typeText, out key.TypeId) == false)
                    {
                        Console.WriteLine("Failed to parse resource key [{0}, {1}, {2}]!",
                            groupText,
                            instanceText,
                            typeText);
                        continue;
                    }

                    string inputPath;
                    if (Path.IsPathRooted(nodes.Current.Value) == false)
                    {
                        // relative path, it should be relative to the XML file
                        inputPath = Path.Combine(filesBasePath, nodes.Current.Value);
                    }
                    else
                    {
                        inputPath = nodes.Current.Value;
                    }

                    if (File.Exists(inputPath) == false)
                    {
                        Console.WriteLine(inputPath + " does not exist!");
                        continue;
                    }

                    using (var input = File.OpenRead(inputPath))
                    {
                        byte[] data = new byte[input.Length];
                        input.Read(data, 0, data.Length);
                        db.SetResource(key, data);
                        input.Close();
                    }
                }

                db.Commit(true);
            }
        }

        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }
    }
}
