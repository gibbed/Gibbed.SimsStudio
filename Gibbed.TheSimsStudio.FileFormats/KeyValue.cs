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

namespace Gibbed.TheSimsStudio.FileFormats
{
    public class KeyValue
    {
        private static KeyValue Invalid = new KeyValue()
        {
            IsValid = false,
        };

        private bool IsValid;
        public string Value;
        public Dictionary<string, KeyValue> Values = null;

        public KeyValue()
            : this(null)
        {
        }

        public KeyValue(string value)
        {
            this.Value = value;
            this.IsValid = true;
        }

        public KeyValue this[string id]
        {
            get
            {
                if (this.Values == null)
                {
                    return Invalid;
                }
                else if (this.Values.ContainsKey(id) == false)
                {
                    return Invalid;
                }

                return this.Values[id];
            }

            set
            {
                if (this.Values == null)
                {
                    if (value != null)
                    {
                        this.Values = new Dictionary<string, KeyValue>();
                        this.Values[id] = value;
                    }
                }
                else
                {
                    if (value == null)
                    {
                        this.Values.Remove(id);
                    }
                    else
                    {
                        this.Values[id] = value;
                    }
                }
            }
        }

        public TType As<TType>()
        {
            return this.As<TType>(default(TType));
        }

        public TType As<TType>(TType defaultValue)
        {
            if (this.IsValid == false)
            {
                return defaultValue;
            }
            else if (this.Value == null)
            {
                return defaultValue;
            }

            return (TType)Convert.ChangeType(this.Value, typeof(TType));
        }
    }
}
