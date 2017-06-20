using System;
using System.Collections.Generic;
using System.Text;

namespace Guytp.BurstSharp.BurstLib
{
    public static class StringExtensions
    {
        public static byte[] ToByteArray(this string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return null;
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
    }
}
