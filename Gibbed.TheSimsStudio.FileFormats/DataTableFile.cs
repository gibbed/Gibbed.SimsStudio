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
using System.Linq;
using System.Text;
using Gibbed.Helpers;

namespace Gibbed.TheSimsStudio.FileFormats
{
    /* This format is dumb as hell.
     * 
     * Why would you bother optimizing lookup if you don't bother
     * storing all data in binary form too?
     * 
     * also the way children that have values are stored is :wtc:
     */
    public class DataTableFile
    {
        public List<Node> Default = null;
        public List<Node> Root = null;

        public void Serialize(Stream output)
        {
            ushort defaultIndex = 0;

            var nodes = new List<NodeHeader>();

            if (this.Root != null)
            {
                SerializeNodes(this.Root, nodes);
            }

            if (this.Default != null && this.Default.Count > 0)
            {
                defaultIndex = (ushort)nodes.Count;
                SerializeNodes(this.Default, nodes);
            }

            var values = new List<string>();
            foreach (var node in nodes)
            {
                if (node.Value != null &&
                    values.Contains(node.Value) == false)
                {
                    values.Add(node.Value);
                }
            }
            values.Sort();

            if (values.Count > 0xFFFF)
            {
                throw new InvalidOperationException();
            }

            foreach (var node in nodes)
            {
                if (node.Value != null)
                {
                    node.ValueIndex = (ushort)values.IndexOf(node.Value);
                }
            }

            output.WriteValueU32(0x44545442);
            output.WriteValueU32(1);
            output.WriteValueS32(nodes.Count);
            output.WriteValueS32(values.Count);
            output.WriteValueU16(defaultIndex);

            foreach (var node in nodes)
            {
                node.Serialize(output);
            }

            foreach (var value in values)
            {
                output.WriteValueS32Packed(value.Length);
                output.WriteString(value, Encoding.UTF8);
            }
        }

        private static void SerializeNodes(List<Node> parents, List<NodeHeader> nodes)
        {
            var children = parents
                .OrderBy(c => c.Name.HashFNV32())
                .ToArray();

            if (children.Length >= 0x7FFF)
            {
                throw new InvalidOperationException();
            }

            if (children.Length == 0)
            {
                return;
            }

            var siblings = new NodeHeader[children.Length];
            for (int i = 0; i < children.Length; i++)
            {
                siblings[i] = new NodeHeader();
                nodes.Add(siblings[i]);
            }

            for (int i = 0; i < children.Length; i++)
            {
                // link up siblings
                if (i + 1 >= children.Length)
                {
                    siblings[i].NextSibling = 0;
                }
                else
                {
                    siblings[i].NextSibling = (ushort)nodes.IndexOf(siblings[i + 1]);
                }
            }

            for (int i = 0; i < children.Length; i++)
            {
                siblings[i].Value = children[i].Name;
                siblings[i].Hash = children[i].Name.HashFNV32();
                SerializeNode(children[i], siblings[i], nodes);
            }
        }

        private static void SerializeNode(Node parent, NodeHeader node, List<NodeHeader> nodes)
        {
            if (nodes.Count >= 0x7FFF)
            {
                throw new InvalidOperationException();
            }

            if (parent.Children == null)
            {
                // don't duplicate value nodes
                // this could be optimized, but for now...

                var value = nodes.FirstOrDefault(
                    n =>
                        n.IsValueNode == true &&
                        n.Value == parent.Value);
                if (value != null)
                {
                    node.FirstChild = (ushort)nodes.IndexOf(value);
                    node.ChildCount = 0x8001;
                }
                else
                {
                    node.FirstChild = (ushort)nodes.Count;
                    node.ChildCount = 0x8001;
                    nodes.Add(new NodeHeader()
                        {
                            IsValueNode = true,
                            Value = parent.Value,

                            Hash = parent.Value.HashFNV32(),
                            ValueIndex = 0,
                            FirstChild = 0,
                            NextSibling = 0,
                            ChildCount = 0,
                        });
                }
            }
            else
            {
                if (parent.Children.Count >= 0x7FFF)
                {
                    throw new InvalidOperationException();
                }

                node.FirstChild = (ushort)nodes.Count;
                node.ChildCount = (ushort)parent.Children.Count;

                if (parent.Children.Count > 0)
                {
                    node.ChildCount = (ushort)parent.Children.Count;
                    SerializeNodes(parent.Children, nodes);
                }
            }
        }

        public void Deserialize(Stream input)
        {
            long basePosition = input.Position;

            if (input.ReadValueU32() != 0x44545442) // DTTB 'DaTaTaBle'
            {
                throw new FormatException("not a data table");
            }

            uint version = input.ReadValueU32();
            if (version != 1)
            {
                throw new FormatException("unsupported data table version");
            }

            var nodeCount = input.ReadValueU32();
            var valueCount = input.ReadValueU32();
            var defaultIndex = input.ReadValueU16();

            // preload strings
            input.Seek((basePosition + 0x12) + (nodeCount * 12), SeekOrigin.Begin);
            var strings = new string[valueCount];
            for (int i = 0; i < strings.Length; i++)
            {
                int length = input.ReadValueS32Packed();
                strings[i] = input.ReadString(length, Encoding.UTF8);
            }

            input.Seek((basePosition + 0x12), SeekOrigin.Begin);
            var nodes = new NodeHeader[nodeCount];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = new NodeHeader();
                nodes[i].Deserialize(input);
                nodes[i].Value = strings[nodes[i].ValueIndex];
                
                if (nodes[i].Hash != nodes[i].Value.HashFNV32())
                {
                    throw new FormatException();
                }
            }

            if (defaultIndex > 0)
            {
                this.Default = DeserializeNodes(nodes, defaultIndex);
            }

            if (nodes.Length > 0)
            {
                this.Root = DeserializeNodes(nodes, 0);
            }
        }

        private static List<Node> DeserializeNodes(
            NodeHeader[] nodes,
            int firstChild)
        {
            var siblings = new List<Node>();
            int nextChild = firstChild;
            do
            {
                siblings.Add(DeserializeNode(nodes, nodes[nextChild]));
                nextChild = nodes[nextChild].NextSibling;
            }
            while (nextChild != 0);
            return siblings;
        }

        private static Node DeserializeNode(
            NodeHeader[] nodes,
            NodeHeader node)
        {
            var parent = new Node();
            parent.Name = node.Value;

            // this node has a value (ie, no child nodes)
            if (node.ChildCount == 0x8001)
            {
                if (node.FirstChild == 0)
                {
                    throw new FormatException();
                }

                if (nodes[node.FirstChild].NextSibling != 0)
                {
                    throw new FormatException();
                }

                parent.Value = nodes[node.FirstChild].Value;
            }
            else
            {
                if (node.ChildCount > 0)
                {
                    if (node.FirstChild == 0)
                    {
                        throw new FormatException();
                    }

                    parent.Children =
                        DeserializeNodes(nodes, node.FirstChild);
                }
                else
                {
                    parent.Children = new List<Node>();
                }
            }

            return parent;
        }

        public class Node
        {
            public string Name;
            public string Value;
            public List<Node> Children = null;

            public KeyValue ToKeyValue()
            {
                if (this.Children == null)
                {
                    return new KeyValue(this.Value);
                }
                else
                {
                    var kv = new KeyValue();
                    foreach (var child in this.Children)
                    {
                        kv[child.Name] = child.ToKeyValue();
                    }
                    return kv;
                }
            }
        }

        private class NodeHeader
        {
            public uint Hash;
            public ushort ValueIndex;
            public ushort FirstChild;
            public ushort NextSibling;
            public ushort ChildCount;

            public string Value;
            public bool IsValueNode = false;

            public void Serialize(Stream output)
            {
                output.WriteValueU32(this.Hash);
                output.WriteValueU16(this.ValueIndex);
                output.WriteValueU16(this.FirstChild);
                output.WriteValueU16(this.NextSibling);
                output.WriteValueU16(this.ChildCount);
            }

            public void Deserialize(Stream input)
            {
                this.Hash = input.ReadValueU32();
                this.ValueIndex = input.ReadValueU16();
                this.FirstChild = input.ReadValueU16();
                this.NextSibling = input.ReadValueU16();
                this.ChildCount = input.ReadValueU16();
            }
        }
    }
}
