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
using System.Text;
using Gibbed.Helpers;

namespace Gibbed.TheSimsStudio.FileFormats.Content
{
    /* Version support:
     * TS3 (1.19.44.010001) : 3 - 18
     * TSM (1.1.10.00001) . : 3 - 22
     */
    public class CASPart
    {
        public uint Version;
        public Dictionary<uint, string> Presets = new Dictionary<uint, string>();
        public string Name;
        public float DisplayIndex;
        public bool Unknown04;
        public uint Unknown05;
        public uint Unknown06;
        public uint Unknown07;
        public uint Unknown08;
        public ResourceKey Unknown09;
        public ResourceKey Unknown10;
        public BlendKeySet BlendKeys;
        public uint Unknown17;
        public List<ResourceKey> VisualProxies = new List<ResourceKey>();
        public List<LODInfo> LODs = new List<LODInfo>();
        public TextureKeySet TextureKeys;
        public List<ResourceKey> Unknown24 = new List<ResourceKey>();
        public string ShoeMaterial;
        public string OutfitMaterial;
        public ulong ProfessionTypes;

        public void Deserialize(Stream input)
        {
            this.Version = input.ReadValueU32();
            if (this.Version < 3 || this.Version > 22)
            {
                throw new FormatException();
            }

            List<ResourceKey> keyTable = null;
            if (this.Version >= 11)
            {
                keyTable = new List<ResourceKey>();

                long keyTableOffset = input.ReadValueU32();
                long originalPosition = input.Position;

                input.Seek(keyTableOffset, SeekOrigin.Current);

                var count = input.ReadValueU8();
                for (var i = 0; i < count; i++)
                {
                    keyTable.Add(input.ReadResourceKeyIGT());
                }

                input.Seek(originalPosition, SeekOrigin.Begin);
            }

            this.Presets.Clear();
            if (this.Version >= 16)
            {
                var count = input.ReadValueU32();
                for (uint i = 0; i < count; i++)
                {
                    var text = input.ReadString(input.ReadValueU32() * 2, true, Encoding.Unicode);
                    var index = input.ReadValueU32();
                    this.Presets.Add(index, text);
                }
            }

            if (this.Version >= 12)
            {
                this.Name = input.ReadString(input.ReadValueS32Packed(), true, Encoding.BigEndianUnicode);
            }

            if (this.Version >= 18)
            {
                this.DisplayIndex = input.ReadValueF32();
            }
            else if (this.Version >= 13)
            {
                input.Seek(4, SeekOrigin.Current); // skip
            }

            this.Unknown04 = input.ReadValueB8();
            this.Unknown05 = input.ReadValueU32();

            if (this.Version >= 15)
            {
                this.Unknown06 = input.ReadValueU32();
            }
            else
            {
                var remap = new uint[] { 0, 1, 2, 4, 8, 8, 8, 8 };
                this.Unknown06 = this.Unknown05 < remap.Length ?
                    remap[this.Unknown05] : 16;
            }

            if (this.Version >= 9)
            {
                this.Unknown06 = input.ReadValueU32();
            }
            else
            {
                var a = input.ReadValueU32();
                var b = input.ReadValueU32();
                this.Unknown06 = a | b;
            }

            if (this.Version >= 8)
            {
                this.Unknown07 = input.ReadValueU32();
            }

            if (this.Version >= 16)
            {
                this.Unknown09 = keyTable[input.ReadValueU8()];
                this.Unknown10 = keyTable[input.ReadValueU8()];
            }
            else if (this.Version >= 15)
            {
                this.Unknown09 = input.ReadResourceKeyIGT();
                this.Unknown10 = input.ReadResourceKeyIGT();
            }

            this.BlendKeys.Deserialize(input, this.Version, keyTable);

            this.Unknown17 = input.ReadValueU32();

            this.VisualProxies.Clear();
            if (this.Version >= 6)
            {
                var count = input.ReadValueU8();
                for (int i = 0; i < count; i++)
                {
                    if (this.Version >= 11)
                    {
                        this.VisualProxies.Add(keyTable[input.ReadValueU8()]);
                    }
                    else
                    {
                        this.VisualProxies.Add(input.ReadResourceKeyTGI());
                    }
                }
            }

            this.LODs.Clear();
            {
                var count = input.ReadValueU8();
                for (int i = 0; i < count; i++)
                {
                    var lodInfo = new LODInfo();
                    lodInfo.Deserialize(input, this.Version);
                    this.LODs.Add(lodInfo);
                }
            }

            this.TextureKeys.Deserialize(input, this.Version, keyTable);

            this.Unknown24.Clear();
            if (this.Version >= 4)
            {
                var count = this.Version >= 6 ? input.ReadValueU8() : 7;
                for (int i = 0; i < count; i++)
                {
                    if (this.Version >= 11)
                    {
                        this.Unknown24.Add(keyTable[input.ReadValueU8()]);
                    }
                    else
                    {
                        this.Unknown24.Add(input.ReadResourceKeyIGT());
                    }
                }
            }

            if (this.Version >= 7 && this.Version <= 15)
            {
                var count = input.ReadValueU32();
                for (uint i = 0; i < count; i++)
                {
                    var text = input.ReadString(input.ReadValueS32Packed(), true, Encoding.BigEndianUnicode);
                    var index = input.ReadValueU32();
                    this.Presets.Add(index, text);
                }
            }

            if (this.Version >= 10)
            {
                this.ShoeMaterial = input.ReadString(input.ReadValueS32Packed(), true, Encoding.BigEndianUnicode);
            }

            if (this.Version >= 22)
            {
                this.OutfitMaterial = input.ReadString(input.ReadValueS32Packed(), true, Encoding.BigEndianUnicode);
            }

            if (this.Version >= 20)
            {
                this.ProfessionTypes = input.ReadValueU64();
            }
            else if (this.Version >= 19)
            {
                this.ProfessionTypes = 0;
                var count = input.ReadValueU32();
                for (uint i = 0; i < count; i++)
                {
                    this.ProfessionTypes |= input.ReadValueU64();
                }
            }
        }

        public class LODInfo
        {
            public byte Level;
            public uint Flags;
            public List<LODAsset> Assets
                = new List<LODAsset>();

            public void Serialize(Stream output, uint version)
            {
                throw new NotImplementedException();
            }

            public void Deserialize(Stream input, uint version)
            {
                this.Level = input.ReadValueU8();
                this.Flags = input.ReadValueU32();

                this.Assets.Clear();
                var count = input.ReadValueU8();
                for (int i = 0; i < count; i++)
                {
                    var asset = new LODAsset();
                    asset.Deserialize(input, version);
                    this.Assets.Add(asset);
                }
            }
        }

        public class LODAsset
        {
            public uint Sorting;
            public uint SpecLevel;
            public uint CastShadow;
            public List<ResourceKey> Unknown3;

            public void Serialize(Stream output, uint version)
            {
                throw new NotImplementedException();
            }

            public void Deserialize(Stream input, uint version)
            {
                this.Sorting = input.ReadValueU32();
                this.SpecLevel = input.ReadValueU32();
                this.CastShadow = input.ReadValueU32();

                if (version <= 5)
                {
                    this.Unknown3 = new List<ResourceKey>();
                    var count = input.ReadValueU8();
                    for (int i = 0; i < count; i++)
                    {
                        this.Unknown3.Add(input.ReadResourceKeyIGT());
                    }
                }
            }
        }

        public struct BlendKeySet
        {
            public ResourceKey Fat; // _fat
            public ResourceKey Fit; // _fit
            public ResourceKey Thin; // _thin
            public ResourceKey Special; // _special
            public ResourceKey Elder; // _elder
            public ResourceKey Teen; // _teen

            public void Deserialize(
                Stream input,
                uint version,
                List<ResourceKey> keyTable)
            {
                if (version >= 17)
                {
                    this.Fat = keyTable[input.ReadValueU8()];
                    this.Fit = keyTable[input.ReadValueU8()];
                    this.Thin = keyTable[input.ReadValueU8()];
                    this.Special = keyTable[input.ReadValueU8()];
                }

                if (version >= 21)
                {
                    this.Elder = keyTable[input.ReadValueU8()];
                    this.Teen = keyTable[input.ReadValueU8()];
                }
            }
        }

        public struct TextureKeySet
        {
            public List<ResourceKey> PrimaryDiffuse;
            public List<ResourceKey> PrimarySpecular;
            public List<ResourceKey> SecondaryDiffuse;
            public List<ResourceKey> SecondarySpecular;

            public void Deserialize(
                Stream input,
                uint version,
                List<ResourceKey> keyTable)
            {
                this.PrimaryDiffuse = new List<ResourceKey>();
                {
                    var count = input.ReadValueU8();
                    for (int i = 0; i < count; i++)
                    {
                        if (version >= 11)
                        {
                            this.PrimaryDiffuse.Add(keyTable[input.ReadValueU8()]);
                        }
                        else
                        {
                            this.PrimaryDiffuse.Add(input.ReadResourceKeyIGT());
                        }
                    }
                }

                this.PrimarySpecular = new List<ResourceKey>();
                {
                    var count = input.ReadValueU8();
                    for (int i = 0; i < count; i++)
                    {
                        if (version >= 11)
                        {
                            this.PrimarySpecular.Add(keyTable[input.ReadValueU8()]);
                        }
                        else
                        {
                            this.PrimarySpecular.Add(input.ReadResourceKeyIGT());
                        }
                    }
                }

                this.SecondaryDiffuse = new List<ResourceKey>();
                if (version >= 5)
                {
                    var count = input.ReadValueU8();
                    for (int i = 0; i < count; i++)
                    {
                        if (version >= 11)
                        {
                            this.SecondaryDiffuse.Add(keyTable[input.ReadValueU8()]);
                        }
                        else
                        {
                            this.SecondaryDiffuse.Add(input.ReadResourceKeyIGT());
                        }
                    }
                }

                this.SecondarySpecular = new List<ResourceKey>();
                if (version >= 5)
                {
                    var count = input.ReadValueU8();
                    for (int i = 0; i < count; i++)
                    {
                        if (version >= 11)
                        {
                            this.SecondarySpecular.Add(keyTable[input.ReadValueU8()]);
                        }
                        else
                        {
                            this.SecondarySpecular.Add(input.ReadResourceKeyIGT());
                        }
                    }
                }
            }
        }
    }
}
