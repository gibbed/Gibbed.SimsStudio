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
using Gibbed.Helpers;

namespace Gibbed.TheSimsStudio.FileFormats.Content
{
    /* Version support:
     * TS3 (1.19.44.010001) : 2 - 6
     * TSM (1.1.10.00001) . : 2 - 4
     */
    public class SkinTone : IFormat
    {
        public uint Version;
        public List<ShaderKey> ShaderKeys = new List<ShaderKey>();
        public ResourceKey SkinRamp;
        public ResourceKey ScatterRamp;
        public List<TextureKey> TextureKeys = new List<TextureKey>();
        public bool Dominant;

        public void Serialize(Stream output)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream input)
        {
            this.Version = input.ReadValueU32();
            if (this.Version < 2 || this.Version > 6)
            {
                throw new FormatException();
            }

            List<ResourceKey> keyTable = null;
            if (this.Version >= 3)
            {
                keyTable = new List<ResourceKey>();

                long keyTableOffset = input.ReadValueU32();
                long originalPosition = input.Position;
                long keyTableSize = input.ReadValueU32();

                input.Seek(originalPosition + keyTableOffset, SeekOrigin.Begin);

                var count = input.ReadValueU32();
                for (uint i = 0; i < count; i++)
                {
                    keyTable.Add(input.ReadResourceKeyTGI());
                }

                input.Seek(originalPosition + 4, SeekOrigin.Begin);
            }

            this.ShaderKeys.Clear();
            {
                var count = input.ReadValueU32();
                for (uint i = 0; i < count; i++)
                {
                    var shaderKey = new ShaderKey();
                    shaderKey.Deserialize(input);
                    this.ShaderKeys.Add(shaderKey);
                }
            }

            if (this.Version >= 3)
            {
                this.SkinRamp = keyTable[input.ReadValueS32()];
                this.ScatterRamp = keyTable[input.ReadValueS32()];
            }
            else
            {
                this.SkinRamp = input.ReadResourceKeyIGT();
                this.ScatterRamp = input.ReadResourceKeyIGT();
            }

            this.TextureKeys.Clear();
            {
                var count = input.ReadValueU32();
                for (int i = 0; i < count; i++)
                {
                    var textureKey = new TextureKey();
                    textureKey.Deserialize(input, this.Version, keyTable);
                    this.TextureKeys.Add(textureKey);
                }
            }
        }

        public class ShaderKey
        {
            public uint AgeGender;
            public uint EdgeColor;
            public uint SpecularColor;
            public float SpecularPower;
            public bool Genetic;

            public void Serialize(Stream output)
            {
                output.WriteValueU32(this.AgeGender);
                output.WriteValueU32(this.EdgeColor);
                output.WriteValueU32(this.SpecularColor);
                output.WriteValueF32(this.SpecularPower);
                output.WriteValueB8(this.Genetic);
            }

            public void Deserialize(Stream input)
            {
                this.AgeGender = input.ReadValueU32();
                this.EdgeColor = input.ReadValueU32();
                this.SpecularColor = input.ReadValueU32();
                this.SpecularPower = input.ReadValueF32();
                this.Genetic = input.ReadValueB8();
            }
        }

        public class TextureKey
        {
            public uint AgeGenderFlags;
            public CASPartFlags PartFlag;
            public ResourceKey SpecMask;
            public ResourceKey DetailDark;
            public ResourceKey DetailLight;
            public ResourceKey NormalMap;
            public ResourceKey Overlay;
            public ResourceKey Unknown7;
            public ResourceKey Unknown8;

            public void Deserialize(Stream input, uint version, List<ResourceKey> keyTable)
            {
                this.AgeGenderFlags = input.ReadValueU32();
                this.PartFlag = (CASPartFlags)input.ReadValueU32();

                if (version >= 3)
                {
                    this.SpecMask = keyTable[input.ReadValueS32()];
                    this.DetailDark = keyTable[input.ReadValueS32()];
                    this.DetailLight = keyTable[input.ReadValueS32()];
                    this.NormalMap = keyTable[input.ReadValueS32()];
                }
                else
                {
                    this.SpecMask = input.ReadResourceKeyIGT();
                    this.DetailDark = input.ReadResourceKeyIGT();
                    this.DetailLight = input.ReadResourceKeyIGT();
                    this.NormalMap = input.ReadResourceKeyIGT();
                }

                this.Overlay = version >= 4 ? keyTable[input.ReadValueS32()] : default(ResourceKey);
                this.Unknown7 = version >= 5 ? keyTable[input.ReadValueS32()] : default(ResourceKey);
                this.Unknown8 = version >= 6 ? keyTable[input.ReadValueS32()] : default(ResourceKey);
            }
        }
    }
}
