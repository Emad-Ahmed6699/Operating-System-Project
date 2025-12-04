using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fat_file_system_cs
{
    internal class DirectoryEntry
    {
        public const int ENTRY_SIZE = 32;
        public const int NAME_LEN = 11;
        
        public string Name11 { get; set; } = new string(' ', NAME_LEN); 
        public byte Attribute { get; set; } = 0;  
        public int FirstCluster { get; set; } = 0;
        public int FileSize { get; set; } = 0;

        public DirectoryEntry() { }

       
        public byte[] ToBytes()
        {
            byte[] b = new byte[ENTRY_SIZE];

            
            var nameBytes = Encoding.ASCII.GetBytes(Name11.PadRight(NAME_LEN, ' '));
            Array.Copy(nameBytes, 0, b, 0, NAME_LEN);

            b[11] = Attribute;

            Array.Copy(BitConverter.GetBytes(FirstCluster), 0, b, 12, 4);
            
            Array.Copy(BitConverter.GetBytes(FileSize), 0, b, 16, 4);

            return b;
        }

        public static DirectoryEntry? FromBytes(byte[] data)
        {
            if (data == null || data.Length != ENTRY_SIZE) throw new ArgumentException("Invalid entry buffer");

            if (data[0] == 0x00) return null;

            string rawName = Encoding.ASCII.GetString(data, 0, NAME_LEN);
            byte attr = data[11];
            int first = BitConverter.ToInt32(data, 12);
            int size = BitConverter.ToInt32(data, 16);

            return new DirectoryEntry
            {
                Name11 = rawName,
                Attribute = attr,
                FirstCluster = first,
                FileSize = size
            };
        }

        public string GetDisplayName()
        {
            string baseName = Name11.Substring(0, 8).Trim();
            string ext = Name11.Substring(8, 3).Trim();
            return string.IsNullOrEmpty(ext) ? baseName : $"{baseName}.{ext}";
        }
    }
}
