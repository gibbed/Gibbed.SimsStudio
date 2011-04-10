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
using System.IO;
using Gibbed.Helpers;

namespace Gibbed.TheSimsStudio.FileFormats
{
    public static class StreamHelpers
    {
        public static Int32 ReadValueS32Packed(this Stream stream)
        {
            int value = 0;
            int shift = 0;
            byte b;

            // packed length
            do
            {
                if (shift > 32)
                {
                    throw new InvalidOperationException();
                }

                b = stream.ReadValueU8();
                value |= (b & 0x7F) << shift;
                shift += 7;
            }
            while ((b & 0x80) == 0x80);

            return value;
        }

        public static void WriteValueS32Packed(this Stream stream, Int32 value)
        {
            do
            {
                var b = (byte)(value & 0x7F);
                if (value > 0x7F)
                {
                    b |= 0x80;
                }

                stream.WriteValueU8(b);
                value >>= 7;
            }
            while (value > 0);
        }
    }
}
