#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System.Runtime.CompilerServices;

namespace Back2Britain.Utility
{
    public static class StringHelper
    {
        private static readonly char[] _dots = { '.', ',', ';', '!' };

        public static string Cp1252ToString(ReadOnlySpan<byte> strCp1252)
        {
            var sb = new ValueStringBuilder(strCp1252.Length);

            for (int i = 0; i < strCp1252.Length; ++i)
            {
                sb.Append(char.ConvertFromUtf32(Cp1252ToUnicode(strCp1252[i])));
            }

            var str = sb.ToString();

            sb.Dispose();

            return str;
        }

        /// <summary>
        /// Converts a cp1252 code point into a unicode code point
        /// </summary>
        private static int Cp1252ToUnicode(byte codepoint)
        {
            switch (codepoint)
            {
                case 128: return 0x20AC; //€
                case 130: return 0x201A; //‚
                case 131: return 0x0192; //ƒ
                case 132: return 0x201E; //„
                case 133: return 0x2026; //…
                case 134: return 0x2020; //†
                case 135: return 0x2021; //‡
                case 136: return 0x02C6; //ˆ
                case 137: return 0x2030; //‰
                case 138: return 0x0160; //Š
                case 139: return 0x2039; //‹
                case 140: return 0x0152; //Œ
                case 142: return 0x017D; //Ž
                case 145: return 0x2018; //‘
                case 146: return 0x2019; //’
                case 147: return 0x201C; //“
                case 148: return 0x201D; //”
                case 149: return 0x2022; //•
                case 150: return 0x2013; //–
                case 151: return 0x2014; //—
                case 152: return 0x02DC; //˜
                case 153: return 0x2122; //™
                case 154: return 0x0161; //š
                case 155: return 0x203A; //›
                case 156: return 0x0153; //œ
                case 158: return 0x017E; //ž
                case 159: return 0x0178; //Ÿ
                default: return codepoint;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSafeChar(int c)
        {
            return c >= 0x20 && c < 0xFFFE;
        }
    }
}