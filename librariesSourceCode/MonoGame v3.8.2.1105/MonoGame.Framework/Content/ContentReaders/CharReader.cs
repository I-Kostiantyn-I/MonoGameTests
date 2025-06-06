// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Content;

namespace Microsoft.Xna.Framework.Content
{
	internal class CharReader : ContentTypeReader<char>
    {
        public CharReader()
        {
        }

        protected internal override char Read(ContentReader input, char existingInstance)
        {
            return input.ReadChar();
        }
    }
}
