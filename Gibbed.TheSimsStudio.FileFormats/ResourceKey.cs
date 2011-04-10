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

using System.IO;
using Gibbed.Helpers;

namespace Gibbed.TheSimsStudio.FileFormats
{
    public static partial class ResourceKeyHelper
    {
        public static ResourceKey ReadResourceKeyTGI(this Stream stream)
        {
            var key = new ResourceKey();
            key.TypeId = stream.ReadValueU32();
            key.GroupId = stream.ReadValueU32();
            key.InstanceId = stream.ReadValueU64();
            return key;
        }

        public static void WriteResourceKeyTGI(this Stream stream, ResourceKey key)
        {
            stream.WriteValueU32(key.TypeId);
            stream.WriteValueU32(key.GroupId);
            stream.WriteValueU64(key.InstanceId);
        }

        public static ResourceKey ReadResourceKeyIGT(this Stream stream)
        {
            var key = new ResourceKey();
            key.InstanceId = stream.ReadValueU64();
            key.GroupId = stream.ReadValueU32();
            key.TypeId = stream.ReadValueU32();
            return key;
        }

        public static void WriteResourceKeyIGT(this Stream stream, ResourceKey key)
        {
            stream.WriteValueU64(key.InstanceId);
            stream.WriteValueU32(key.GroupId);
            stream.WriteValueU32(key.TypeId);
        }
    }

    public struct ResourceKey
    {
        public uint TypeId;
        public uint GroupId;
        public ulong InstanceId;

        public ResourceKey(ulong instanceId, uint typeId, uint groupId)
        {
            this.InstanceId = instanceId;
            this.TypeId = typeId;
            this.GroupId = groupId;
        }

        public ResourceKey(ulong instanceId, uint typeId)
            : this(instanceId, typeId, 0)
        {
        }

        public ResourceKey(string instance, uint typeId, uint groupId)
            : this(instance.HashFNV64(), typeId, groupId)
        {
        }

        public ResourceKey(string instance, uint typeId)
            : this(instance, typeId, 0)
        {
        }

        public string ToPath()
        {
            return string.Format("{0:X8}-{1:X8}-{2:X16}",
                this.TypeId, this.GroupId, this.InstanceId);
        }

        public override string ToString()
        {
            return string.Format("{0:X8}:{1:X8}:{2:X16}",
                this.TypeId, this.GroupId, this.InstanceId);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != this.GetType())
            {
                return false;
            }

            return (ResourceKey)obj == this;
        }

        public static bool operator !=(ResourceKey a, ResourceKey b)
        {
            return
                a.TypeId != b.TypeId ||
                a.GroupId != b.GroupId ||
                a.InstanceId != b.InstanceId;
        }

        public static bool operator ==(ResourceKey a, ResourceKey b)
        {
            return
                a.TypeId == b.TypeId &&
                a.GroupId == b.GroupId &&
                a.InstanceId == b.InstanceId;
        }

        public override int GetHashCode()
        {
            return this.InstanceId.GetHashCode() ^ ((int)(this.TypeId ^ (this.GroupId << 16)));
        }
    }
}
