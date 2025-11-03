using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fat_file_system_cs
{
    internal class FatTableManager
    {
        private readonly VirtualDisk disk;
        private readonly int[] fat = new int[FsConstants.CLUSTER_COUNT];

        public FatTableManager(VirtualDisk virtualDisk)
        {
            disk = virtualDisk ?? throw new ArgumentNullException(nameof(virtualDisk));
        }

        // 🟢 1. Load FAT from disk (clusters 1 → 4)
        public void LoadFatFromDisk()
        {
            byte[] buffer = new byte[FsConstants.FAT_CLUSTERS * FsConstants.CLUSTER_SIZE];

            for (int i = 0; i < FsConstants.FAT_CLUSTERS; i++)
            {
                byte[] clusterData = disk.ReadCluster(FsConstants.FAT_START_CLUSTER + i);
                Array.Copy(clusterData, 0, buffer, i * FsConstants.CLUSTER_SIZE, FsConstants.CLUSTER_SIZE);
            }

            Buffer.BlockCopy(buffer, 0, fat, 0, buffer.Length);
        }

        // 🔵 2. Flush FAT back to disk (write 4 clusters)
        public void FlushFatToDisk()
        {
            byte[] buffer = new byte[FsConstants.FAT_CLUSTERS * FsConstants.CLUSTER_SIZE];
            Buffer.BlockCopy(fat, 0, buffer, 0, buffer.Length);

            for (int i = 0; i < FsConstants.FAT_CLUSTERS; i++)
            {
                byte[] clusterData = new byte[FsConstants.CLUSTER_SIZE];
                Array.Copy(buffer, i * FsConstants.CLUSTER_SIZE, clusterData, 0, FsConstants.CLUSTER_SIZE);
                disk.WriteCluster(FsConstants.FAT_START_CLUSTER + i, clusterData);
            }
        }

        // 🟣 3. Get FAT entry value
        public int GetFatEntry(int index)
        {
            ValidateIndex(index);
            return fat[index];
        }

        // 🟣 4. Set FAT entry value
        public void SetFatEntry(int index, int value)
        {
            ValidateIndex(index);
            fat[index] = value;
        }

        // 🟡 5. Read the whole FAT
        public int[] ReadAllFat() => fat;

        // 🟡 6. Write the whole FAT
        public void WriteAllFat(int[] entries)
        {
            if (entries.Length != fat.Length)
                throw new ArgumentException("Invalid FAT length");
            Array.Copy(entries, fat, fat.Length);
        }

        // 🔴 7. Follow a chain of clusters
        public List<int> FollowChain(int start)
        {
            ValidateIndex(start);
            List<int> chain = new List<int>();
            int current = start;

            while (current != -1)
            {
                chain.Add(current);
                current = fat[current];
            }
            return chain;
        }

        // 🟢 8. Allocate new chain
        public int AllocateChain(int count)
        {
            List<int> freeClusters = new List<int>();

            for (int i = FsConstants.CONTENT_START_CLUSTER; i < fat.Length && freeClusters.Count < count; i++)
            {
                if (fat[i] == 0)
                    freeClusters.Add(i);
            }

            if (freeClusters.Count < count)
                throw new Exception("Not enough free clusters!");

            for (int i = 0; i < freeClusters.Count - 1; i++)
                fat[freeClusters[i]] = freeClusters[i + 1];

            fat[freeClusters[^1]] = -1; // Last cluster = end of chain

            return freeClusters[0]; // Return first cluster
        }

        // 🔵 9. Free a chain (release all clusters)
        public void FreeChain(int start)
        {
            int current = start;

            while (current != -1)
            {
                int next = fat[current];
                fat[current] = 0;
                current = next;
            }
        }

        // 🛠️ Helper to prevent invalid access
        private void ValidateIndex(int index)
        {
            if (index < 0 || index >= FsConstants.CLUSTER_COUNT)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (index <= FsConstants.FAT_END_CLUSTER)
                throw new InvalidOperationException("Reserved cluster!");
        }
    }
}
