using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fat_file_system_cs
{
    internal class SuperblockManager
    {
        private readonly VirtualDisk disk;
        private byte[] superblockData;

        public SuperblockManager(VirtualDisk virtualDisk)
        {
            disk = virtualDisk ?? throw new ArgumentNullException(nameof(virtualDisk));

            superblockData = new byte[FsConstants.CLUSTER_SIZE];

            for (int i = 0; i < FsConstants.CLUSTER_SIZE; i++)
                superblockData[i] = 0;

            disk.WriteCluster(FsConstants.SUPERBLOCK_CLUSTER, superblockData);
        }

        public byte[] ReadSuperblock()
        {
            return disk.ReadCluster(FsConstants.SUPERBLOCK_CLUSTER);
        }

        public void WriteSuperblock(byte[] data)
        {
            if (data == null || data.Length != FsConstants.CLUSTER_SIZE)
                throw new ArgumentException($"Superblock data must be {FsConstants.CLUSTER_SIZE} bytes.");

            disk.WriteCluster(FsConstants.SUPERBLOCK_CLUSTER, data);
        }

    }
}
