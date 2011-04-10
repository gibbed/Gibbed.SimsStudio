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
using Gibbed.TheSimsStudio.FileFormats;

namespace Gibbed.TheSimsStudio.Data.ResourceSources
{
    public class DirectorySource : IResourceSource
    {
        private readonly string InputPath;
        public Dictionary<ResourceKey, string> Files
            = new Dictionary<ResourceKey, string>();

        public override string ToString()
        {
            return this.InputPath;
        }

        public DirectorySource(string inputPath)
        {
            this.InputPath = inputPath;
        }

        public void Load(Dictionary<string, uint> types)
        {
            foreach (var kvp in types)
            {
                foreach (var filePath in Directory.GetFiles(
                    this.InputPath, "*." + kvp.Key))
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var fileKey = new ResourceKey(fileName, kvp.Value);
                    this.Files.Add(fileKey, filePath);
                }
            }
        }

        public bool ContainsResource(ResourceKey key)
        {
            return this.Files.ContainsKey(key) == true;
        }

        public Stream LoadResource(ResourceKey key)
        {
            if (this.Files.ContainsKey(key) == false)
            {
                return null;
            }

            using (var input = File.OpenRead(this.Files[key]))
            {
                return input.ReadToMemoryStream(input.Length);
            }
        }

        public IEnumerable<ResourceKey> FindResourcesOfType(uint typeId)
        {
            return this.Files
                .Where(f => f.Key.TypeId == typeId)
                .Select(f => f.Key);
        }

        public IEnumerator<ResourceKey> GetEnumerator()
        {
            return this.Files.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
