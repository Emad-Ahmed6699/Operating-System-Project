using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fat_file_system_cs
{
    internal static class FsConstants
    {
        public const int CLUSTER_SIZE = 1024;

        public const int CLUSTER_COUNT = 1024;

        public const int SUPERBLOCK_CLUSTER = 0;

        public const int FAT_START_CLUSTER = 1;
        public const int FAT_END_CLUSTER = 4;


        public const int FAT_CLUSTERS = FAT_END_CLUSTER - FAT_START_CLUSTER + 1; // = 4
        public const int FAT_BYTES = FAT_CLUSTERS * CLUSTER_SIZE;               // = 4096
        public const int FAT_ENTRY_SIZE = 4;                                    // 4 bytes per FAT entry (int)
        public const int FAT_ENTRIES = FAT_BYTES / FAT_ENTRY_SIZE;//1024


        public const int ROOT_DIR_FIRST_CLUSTER = 5;

        public const int CONTENT_START_CLUSTER = 6;

        public const byte ATTR_FILE = 0x01;
        public const byte ATTR_DIRECTORY = 0x10;
    }
}
