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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gibbed.TheSimsStudio.FileFormats;

namespace Gibbed.TheSimsStudio.Data
{
    public class ResourceLoader
    {
        private List<string> ParsedConfigurations;
        private List<PrioritizedSource> Sources;
        private Dictionary<string, uint> Types;
        private bool Readied;

        public ResourceLoader()
        {
            this.ParsedConfigurations = new List<string>();
            this.Sources = new List<PrioritizedSource>();
            this.Types = new Dictionary<string, uint>();
            this.Readied = false;
        }

        public void Ready()
        {
            if (this.Readied == true)
            {
                return;
            }

            this.Readied = true;
            foreach (var source in this.Sources)
            {
                source.Load(this.Types);
            }
        }

        public bool ContainsResource(ResourceKey key)
        {
            if (this.Readied == false)
            {
                throw new InvalidOperationException();
            }

            foreach (var source in this.Sources)
            {
                if (source.ContainsResource(key) == true)
                {
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<IResourceSource> GetResourceSources(ResourceKey key)
        {
            if (this.Readied == false)
            {
                throw new InvalidOperationException();
            }

            return this.Sources
                .Where(s => s.ContainsResource(key) == true)
                .OrderByDescending(s => s.Priority)
                .Select(s => s.Source);
        }

        public Stream LoadResource(ResourceKey key)
        {
            if (this.Readied == false)
            {
                throw new InvalidOperationException();
            }

            foreach (var source in this.Sources
                .OrderByDescending(s => s.Priority))
            {
                if (source.ContainsResource(key) == true)
                {
                    return source.LoadResource(key);
                }
            }

            return null;
        }

        public TType LoadResource<TType>(ResourceKey key)
            where TType : class, IFormat, new()
        {
            var input = this.LoadResource(key);
            if (input == null)
            {
                return null;
            }

            var instance = new TType();
            instance.Deserialize(input);
            return instance;
        }

        public IEnumerable<ResourceKey> FindResourcesOfType(uint typeId)
        {
            if (this.Readied == false)
            {
                throw new InvalidOperationException();
            }

            return this.Sources
                .SelectMany(s => s.FindResourcesOfType(typeId))
                .Distinct();
        }

        public void AddConfiguration(string inputPath)
        {
            if (this.Readied == true)
            {
                throw new InvalidOperationException();
            }

            bool stopScanning;
            ReadConfiguration(inputPath, out stopScanning);
        }

        private void ReadConfiguration(string inputPath, out bool stopScanning)
        {
            if (this.Readied == true)
            {
                throw new InvalidOperationException();
            }

            stopScanning = false;
            inputPath = Path.GetFullPath(inputPath);

            if (this.ParsedConfigurations.Contains(inputPath) == true)
            {
                return;
            }
            this.ParsedConfigurations.Add(inputPath);

            var basePath = Path.GetDirectoryName(inputPath);

            int priority = 0;
            var extraPaths = new Queue<string>();

            using (var input = new StreamReader(inputPath))
            {
                while (input.EndOfStream == false)
                {
                    var line = input.ReadLine();
                    if (string.IsNullOrWhiteSpace(line) == true)
                    {
                        continue;
                    }

                    var tokens = ParseLine(line);
                    if (tokens.Length < 1)
                    {
                        continue;
                    }

                    switch (tokens[0].ToLowerInvariant())
                    {
                        case "stopscan":
                        {
                            stopScanning = true;
                            break;
                        }

                        case "group":
                        {
                            if (tokens.Length < 2 || tokens.Length > 3)
                            {
                                throw new InvalidOperationException();
                            }

                            // not sure how to handle these properly yet, so I'll silently
                            // ignore them...

                            /*
                            uint group = tokens[1] == "reset" ? 0xFFFFFFFF : uint.Parse(tokens[1]);
                            string target = tokens.Length < 3 ? ".../" : tokens[2];

                            throw new NotImplementedException();
                            */

                            break;
                        }

                        case "priority":
                        {
                            if (tokens.Length < 2)
                            {
                                throw new InvalidOperationException();
                            }

                            if (int.TryParse(tokens[1], out priority) == false)
                            {
                                throw new InvalidOperationException();
                            }

                            break;
                        }

                        case "filetype":
                        {
                            if (tokens.Length < 2)
                            {
                                throw new InvalidOperationException();
                            }

                            if (tokens[1].ToLowerInvariant() == "reset")
                            {
                                for (int i = 2; i < tokens.Length; i++)
                                {
                                    var target = tokens[i];
                                    if (this.Types.ContainsKey(target) == true)
                                    {
                                        this.Types.Remove(target);
                                    }
                                }
                            }
                            else
                            {
                                uint type;
                                if (Hex.TryParseU32(tokens[1], out type) == false)
                                {
                                    throw new InvalidOperationException();
                                }

                                for (int i = 2; i < tokens.Length; i++)
                                {
                                    this.Types[tokens[i]] = type;
                                }
                            }

                            break;
                        }

                        case "scan":
                        {
                            if (tokens.Length != 2)
                            {
                                throw new InvalidOperationException();
                            }

                            var paths = GetPaths(basePath, tokens[1]);

                            foreach (var path in paths)
                            {
                                var extraPath = Path.Combine(path, "Resource.cfg");
                                if (File.Exists(extraPath) == true)
                                {
                                    bool shouldStopScanning;
                                    ReadConfiguration(extraPath, out shouldStopScanning);
                                    if (shouldStopScanning == true)
                                    {
                                        break;
                                    }
                                }
                            }

                            break;
                        }

                        case "directoryfiles":
                        {
                            if (tokens.Length < 2)
                            {
                                throw new InvalidOperationException();
                            }

                            bool autoupdate = false;
                            for (int i = 2; i < tokens.Length; i++)
                            {
                                if (tokens[i].ToLowerInvariant() == "autoupdate")
                                {
                                    autoupdate = true;
                                }
                            }

                            var targetPath = GetPath(basePath, tokens[1]);
                            if (Directory.Exists(targetPath))
                            {
                                this.Sources.Add(new PrioritizedSource()
                                    {
                                        Priority = priority,
                                        Source = new ResourceSources.DirectorySource(targetPath),
                                    });
                            }

                            break;
                        }

                        case "packedfile":
                        {
                            if (tokens.Length < 2)
                            {
                                throw new InvalidOperationException();
                            }

                            bool writable = false;
                            for (int i = 2; i < tokens.Length; i++)
                            {
                                if (tokens[i].ToLowerInvariant() == "writable")
                                {
                                    writable = true;
                                }
                            }

                            var paths = GetPaths(basePath, tokens[1]);
                            var filter = paths[paths.Length - 1];

                            for (int i = 0; i < paths.Length - 1; i++)
                            {
                                var extraPath = Path.Combine(paths[i], "Resource.cfg");
                                if (extraPaths.Contains(extraPath) == false &&
                                    File.Exists(extraPath) == true)
                                {
                                    extraPaths.Enqueue(extraPath);
                                }
                            }

                            var filterPath = Path.GetDirectoryName(filter);
                            var filterName = Path.GetFileName(filter);
                            if (Directory.Exists(filterPath) == true)
                            {
                                foreach (var targetPath in Directory.GetFiles(filterPath, filterName))
                                {
                                    this.Sources.Add(new PrioritizedSource()
                                        {
                                            Priority = priority,
                                            Source = new ResourceSources.PackageSource(targetPath),
                                        });
                                }
                            }
                            
                            break;
                        }

                        case "select":
                        {
                            throw new NotImplementedException();
                        }

                        case "end":
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
            }

            while (extraPaths.Count > 0)
            {
                var extraPath = extraPaths.Dequeue();
                bool shouldStopScanning;
                this.AddConfiguration(extraPath);
            }
        }

        private struct PrioritizedSource : IResourceSource
        {
            public int Priority;
            public IResourceSource Source;

            public override string ToString()
            {
                return string.Format("{0}: {1}", this.Priority, this.Source);
            }

            public void Load(Dictionary<string, uint> types)
            {
                this.Source.Load(types);
            }

            public bool ContainsResource(ResourceKey key)
            {
                return this.Source.ContainsResource(key);
            }

            public Stream LoadResource(ResourceKey key)
            {
                return this.Source.LoadResource(key);
            }

            public IEnumerable<ResourceKey> FindResourcesOfType(uint typeId)
            {
                return this.Source.FindResourcesOfType(typeId);
            }
            
            public IEnumerator<ResourceKey> GetEnumerator()
            {
                return this.Source.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.Source.GetEnumerator();
            }
        }

        private static string GetPath(string basePath, string relativePath)
        {
            relativePath = relativePath.Replace('/', '\\');

            if (Path.IsPathRooted(relativePath) == true)
            {
                throw new InvalidOperationException();
            }

            var parts = relativePath.Split(
                new char[] { '\\' },
                StringSplitOptions.RemoveEmptyEntries);
            var currentPath = basePath;

            foreach (var part in parts)
            {
                if (part == ".")
                {
                    // nothing
                }
                else if (part == "..")
                {
                    currentPath = Path.GetDirectoryName(currentPath);
                }
                else if (part == "...")
                {
                    //throw new NotSupportedException();
                    break;
                }
                else
                {
                    currentPath = Path.Combine(currentPath, part);
                }
            }

            return currentPath;
        }
        private static string[] GetPaths(string basePath, string relativePath)
        {
            var paths = new List<string>();

            relativePath = relativePath.Replace('/', '\\');

            if (Path.IsPathRooted(relativePath) == true)
            {
                throw new InvalidOperationException();
            }

            var parts = relativePath.Split(
                new char[] { '\\' },
                StringSplitOptions.RemoveEmptyEntries);
            var currentPath = basePath;

            foreach (var part in parts)
            {
                if (part == ".")
                {
                    // nothing
                }
                else if (part == "..")
                {
                    currentPath = Path.GetDirectoryName(currentPath);
                }
                else if (part == "...")
                {
                    throw new NotSupportedException();
                }
                else
                {
                    currentPath = Path.Combine(currentPath, part);
                }

                paths.Add(currentPath);
            }

            return paths.ToArray();
        }
        private static string[] ParseLine(string line)
        {
            var tokens = new List<string>();

            for (int start = 0; start < line.Length; )
            {
                for (; start < line.Length; start++)
                {
                    if (char.IsWhiteSpace(line[start]) == false)
                    {
                        break;
                    }
                }

                if (line[start] == '#')
                {
                    break;
                }

                if (line[start] == '"' || line[start] == '\'')
                {
                    char quote = line[start];

                    int end;
                    for (end = start + 1; end < line.Length; end++)
                    {
                        if (line[end] == quote)
                        {
                            break;
                        }
                    }

                    tokens.Add(line.Substring(start + 1, end - start - 1));
                    start = end + 1;
                }
                else
                {
                    int end;
                    for (end = start + 1; end < line.Length; end++)
                    {
                        if (char.IsWhiteSpace(line[end]) == true)
                        {
                            break;
                        }
                    }

                    tokens.Add(line.Substring(start, end - start));
                    start = end + 1;
                }
            }

            return tokens.ToArray();
        }
    }
}
