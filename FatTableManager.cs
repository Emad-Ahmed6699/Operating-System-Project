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

        public void LoadFatFromDisk()
        {
            byte[] buffer = new byte[FsConstants.FAT_CLUSTERS * FsConstants.CLUSTER_SIZE];

            for (int i = 0; i < FsConstants.FAT_CLUSTERS; i++)
            {
                byte[] clusterData = disk.ReadCluster(FsConstants.FAT_START_CLUSTER + i);
                Array.Copy(clusterData, 0, buffer, i * FsConstants.CLUSTER_SIZE, FsConstants.CLUSTER_SIZE);
            }

            for (int i = 0; i < FsConstants.CLUSTER_COUNT; i++)
            {
                fat[i] = BitConverter.ToInt32(buffer, i * 4);
            }
        }

        public void FlushFatToDisk()
        {
            byte[] buffer = new byte[FsConstants.FAT_CLUSTERS * FsConstants.CLUSTER_SIZE];

            for (int i = 0; i < FsConstants.CLUSTER_COUNT; i++)
            {
                byte[] intBytes = BitConverter.GetBytes(fat[i]);
                Array.Copy(intBytes, 0, buffer, i * 4, 4);
            }

            for (int i = 0; i < FsConstants.FAT_CLUSTERS; i++)
            {
                byte[] clusterData = new byte[FsConstants.CLUSTER_SIZE];
                Array.Copy(buffer, i * FsConstants.CLUSTER_SIZE, clusterData, 0, FsConstants.CLUSTER_SIZE);
                disk.WriteCluster(FsConstants.FAT_START_CLUSTER + i, clusterData);
            }
        }

        public int GetFatEntry(int index)
        {
            ValidateIndex(index);
            return fat[index];
        }

        public void SetFatEntry(int index, int value)
        {
            ValidateIndex(index);
            fat[index] = value;
        }

        public int[] ReadAllFat() => fat;

        public void WriteAllFat(int[] entries)
        {
            if (entries.Length != fat.Length)
                throw new ArgumentException("Invalid FAT length");
            Array.Copy(entries, fat, fat.Length);
        }

        public List<int> FollowChain(int start)
        {
            ValidateIndex(start);
            List<int> chain = new List<int>();
            int current = start;

            while (current != -1 && current != 0)
            {
                if (chain.Contains(current))
                    throw new InvalidOperationException("Circular chain detected!");

                chain.Add(current);
                current = fat[current];
            }
            return chain;
        }

        public int AllocateChain(int count)
        {
            List<int> freeClusters = new List<int>();

            for (int i = FsConstants.CONTENT_START_CLUSTER; i < fat.Length && freeClusters.Count < count; i++)
            {
                if (fat[i] == 0)
                    freeClusters.Add(i);
            }

            if (freeClusters.Count < count)
                throw new Exception($"Not enough free clusters! Requested: {count}, Available: {freeClusters.Count}");

            for (int i = 0; i < freeClusters.Count - 1; i++)
                fat[freeClusters[i]] = freeClusters[i + 1];

            fat[freeClusters[^1]] = -1;

            return freeClusters[0];
        }

        public void FreeChain(int start)
        {
            if (start == 0 || start == -1) return;

            int current = start;
            HashSet<int> visited = new HashSet<int>();

            while (current != -1 && current != 0)
            {
                if (visited.Contains(current))
                    throw new InvalidOperationException("Circular chain detected during free!");

                visited.Add(current);
                int next = fat[current];
                fat[current] = 0;
                current = next;
            }
        }

        private void ValidateIndex(int index)
        {
            if (index < 0 || index >= FsConstants.CLUSTER_COUNT)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range [0, {FsConstants.CLUSTER_COUNT})");

            if (index <= FsConstants.FAT_END_CLUSTER)
                throw new InvalidOperationException($"Cluster {index} is reserved (FAT area)!");
        }
    }
}
