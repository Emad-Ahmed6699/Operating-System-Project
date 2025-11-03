using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fat_file_system_cs
{
    internal class Converter
    {
        public static byte[] StringToBytes(string text)
        {
            return System.Text.Encoding.UTF8.GetBytes(text);
        }

        public static string BytesToString(byte[] bytes)
        {
            return System.Text.Encoding.UTF8.GetString(bytes).TrimEnd('\0');
        }
    }
}
