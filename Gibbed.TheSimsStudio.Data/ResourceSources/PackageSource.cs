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

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gibbed.Helpers;
using Gibbed.RefPack;
using Gibbed.TheSimsStudio.FileFormats;

namespace Gibbed.TheSimsStudio.Data.ResourceSources
{
    public class PackageSource : IResourceSource
    {
        private readonly string InputPath;
        private DatabasePackedFile Package;

        public override string ToString()
        {
            return this.InputPath;
        }

        public PackageSource(string inputPath)
        {
            this.InputPath = inputPath;
        }

        public void Load(Dictionary<string, uint> types)
        {
            using (var input = File.OpenRead(this.InputPath))
            {
                this.Package = new DatabasePackedFile();
                this.Package.Read(input);
            }
        }

        public bool ContainsResource(ResourceKey key)
        {
            if (this.Package == null)
            {
                return false;
            }

            return this.Package.Entries
                .FirstOrDefault(e => e.Key == key)
                .Valid == true;
        }

        public Stream LoadResource(ResourceKey key)
        {
            if (this.Package == null)
            {
                return null;
            }

            var entry = this.Package.Entries
                .FirstOrDefault(e => e.Key == key);
            if (entry.Valid == false)
            {
                return null;
            }

            using (var input = File.OpenRead(this.InputPath))
            {
                input.Seek(entry.Offset, SeekOrigin.Begin);

                if (entry.Compressed == true)
                {
                    return new MemoryStream(Decompression.Decompress(input));
                }
                else
                {
                    return input.ReadToMemoryStream(entry.CompressedSize);
                }
            }
        }

        public IEnumerable<ResourceKey> FindResourcesOfType(uint typeId)
        {
            return this.Package.Entries
                .Where(e => e.Key.TypeId == typeId)
                .Select(e => e.Key);
        }

        public IEnumerator<ResourceKey> GetEnumerator()
        {
            if (this.Package == null)
            {
                return new List<ResourceKey>().GetEnumerator();
            }

            return this.Package.Entries
                .Select(e => e.Key)
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
