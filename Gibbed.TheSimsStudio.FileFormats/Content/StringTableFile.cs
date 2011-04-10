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

namespace Gibbed.TheSimsStudio.FileFormats.Content
{
    public class StringTableFile : IFormat
    {
        public uint Magic = 0x4C425453;
        public ushort Version = 2;
        public Dictionary<ulong, string> Strings
            = new Dictionary<ulong, string>();

        public bool LittleEndian { get { return this.Version >= 2; } }
        public Encoding Encoding
        {
            get
            {
                return this.LittleEndian == true ?
                    Encoding.Unicode :
                    Encoding.BigEndianUnicode;
            }
        }

        public void Serialize(Stream output)
        {
            output.WriteValueU32(this.Magic, false);
            output.WriteValueU16(this.Version, true);
            output.Seek(1, SeekOrigin.Current);
            output.WriteValueU64((ulong)this.Strings.Count, this.LittleEndian);
            output.Seek(2, SeekOrigin.Current);

            foreach (var kvp in this.Strings.OrderBy(kv => kv.Key))
            {
                output.WriteValueU64(kvp.Key, this.LittleEndian);
                output.WriteValueS32(kvp.Value.Length, this.LittleEndian);
                output.WriteString(kvp.Value, this.Encoding);
            }
        }

        public void Deserialize(Stream input)
        {
            this.Magic = input.ReadValueU32(false);
            this.Version = input.ReadValueU16(true);
            
            if (this.Version < 1 || this.Version > 2)
            {
                throw new FormatException();
            }

            bool littleEndian = this.Version >= 2;

            //input.ReadValueU8();
            input.Seek(1, SeekOrigin.Current);
            ulong count = input.ReadValueU64(this.LittleEndian);
            /*input.ReadValueU8();
            input.ReadValueU8();*/
            input.Seek(2, SeekOrigin.Current);

            if (count > uint.MaxValue)
            {
                throw new InvalidOperationException("that's retarded");
            }

            this.Strings.Clear();
            for (ulong i = 0; i < count; i++)
            {
                var name = input.ReadValueU64(this.LittleEndian);
                var length = input.ReadValueU32(this.LittleEndian);
                if (length > ushort.MaxValue)
                {
                    throw new InvalidOperationException("that's retarded");
                }
                var text = input.ReadString(length * 2, this.Encoding);

                if (this.Strings.ContainsKey(name) == false)
                {
                    this.Strings[name] = text;
                }
            }
        }
    }
}
