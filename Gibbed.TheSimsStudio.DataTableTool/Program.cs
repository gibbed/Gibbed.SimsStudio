﻿/* Copyright (c) 2011 Rick (rick 'at' gibbed 'dot' us)
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
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using Gibbed.Helpers;
using Gibbed.TheSimsStudio.FileFormats;
using NDesk.Options;

namespace Gibbed.TheSimsStudio.DataTableTool
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

            string inputPath = extras[0];
            string outputPath =
                extras.Count > 1 ? extras[1] : inputPath;

            if (extras.Count == 1 &&
                Directory.Exists(inputPath) == true)
            {
                foreach (var filePath in Directory.GetFiles(inputPath, "*", SearchOption.AllDirectories))
                {
                    Console.WriteLine("{0}", filePath);
                    Convert(filePath, filePath);
                }
            }
            else
            {
                Convert(inputPath, outputPath);
            }
        }

        private static void Convert(string inputPath, string outputPath)
        {
            MemoryStream data;
            using (var input = File.OpenRead(inputPath))
            {
                data = input.ReadToMemoryStream(input.Length);
            }

            using (data)
            {
                var magic = data.ReadValueU32();
                data.Seek(0, SeekOrigin.Begin);

                // detect type
                if (magic == 0x44545442)
                {
                    var dttb = new DataTableFile();
                    dttb.Deserialize(data);

                    var settings = new XmlWriterSettings();
                    settings.Indent = true;

                    using (var writer = XmlWriter.Create(outputPath, settings))
                    {
                        writer.WriteStartDocument();
                        writer.WriteStartElement("datatable");

                        writer.WriteStartElement("default");
                        if (dttb.Default != null)
                        {
                            WriteNodes(writer, dttb.Default);
                        }
                        writer.WriteEndElement();

                        writer.WriteStartElement("root");
                        if (dttb.Root != null)
                        {
                            WriteNodes(writer, dttb.Root);
                        }
                        writer.WriteEndElement();

                        writer.WriteEndElement();
                        writer.WriteEndDocument();
                    }
                }
                else
                {
                    var dttb = new DataTableFile();

                    var xml = new XPathDocument(data);
                    var nav = xml.CreateNavigator();

                    var doc = nav.SelectSingleNode("/datatable");
                    if (doc == null)
                    {
                        throw new InvalidOperationException();
                    }
                    else
                    {
                        var def = doc.SelectSingleNode("default");
                        if (def != null)
                        {
                            dttb.Default = ReadNodes(def.SelectChildren(XPathNodeType.Element));
                        }

                        var root = doc.SelectSingleNode("root");
                        if (root != null)
                        {
                            dttb.Root = ReadNodes(root.SelectChildren(XPathNodeType.Element));
                        }

                        using (var output = File.Create(outputPath))
                        {
                            dttb.Serialize(output);
                        }
                    }
                }
            }
        }

        private static List<DataTableFile.Node> ReadNodes(XPathNodeIterator nav)
        {
            var siblings = new List<DataTableFile.Node>();
            if (nav != null)
            {
                while (nav.MoveNext() == true)
                {
                    siblings.Add(ReadNode(nav.Current));
                }
            }
            return siblings;
        }

        private static DataTableFile.Node ReadNode(XPathNavigator nav)
        {
            var node = new DataTableFile.Node();
            node.Name = nav.Name;

            if (nav.IsEmptyElement == true)
            {
                node.Children = new List<DataTableFile.Node>();
            }
            else
            {
                var children = nav.SelectChildren(XPathNodeType.Element);
                if (children != null && children.Count > 0)
                {
                    node.Children = ReadNodes(children);
                }
                else
                {
                    node.Value = nav.Value;
                }
            }

            return node;
        }

        private static void WriteNodes(XmlWriter writer, List<DataTableFile.Node> siblings)
        {
            foreach (var child in siblings.OrderBy(c => c.Name))
            {
                WriteNode(writer, child);
            }
        }

        private static void WriteNode(XmlWriter writer, DataTableFile.Node parent)
        {
            writer.WriteStartElement(parent.Name);

            if (parent.Children != null)
            {
                WriteNodes(writer, parent.Children);
            }
            else
            {
                writer.WriteValue(parent.Value);
            }

            writer.WriteEndElement();
        }

        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }
    }
}
