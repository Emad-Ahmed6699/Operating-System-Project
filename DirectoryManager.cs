using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fat_file_system_cs
{
    internal class DirEntryLocation
    {
        public DirectoryEntry Entry { get; set; } = null!;
        public int ClusterIndex { get; set; }
        public int EntryIndex { get; set; } 
    }
    internal class DirectoryManager
    {
        private readonly VirtualDisk disk;
        private readonly FatTableManager fat;

        public DirectoryManager(VirtualDisk virtualDisk, FatTableManager fatManager)
        {
            disk = virtualDisk ?? throw new ArgumentNullException(nameof(virtualDisk));
            fat = fatManager ?? throw new ArgumentNullException(nameof(fatManager));
        }

        public List<DirEntryLocation> ReadDirectory(int startCluster)
        {
            if (startCluster < FsConstants.SUPERBLOCK_CLUSTER || startCluster >= FsConstants.CLUSTER_COUNT)
                throw new ArgumentOutOfRangeException(nameof(startCluster));

            var result = new List<DirEntryLocation>();
            var chain = fat.FollowChain(startCluster);

            foreach (var cluster in chain)
            {
                byte[] clusterBytes = disk.ReadCluster(cluster);

                for (int i = 0; i < FsConstants.CLUSTER_SIZE / DirectoryEntry.ENTRY_SIZE; i++)
                {
                    int offset = i * DirectoryEntry.ENTRY_SIZE;
                    byte[] entryBytes = new byte[DirectoryEntry.ENTRY_SIZE];
                    Array.Copy(clusterBytes, offset, entryBytes, 0, DirectoryEntry.ENTRY_SIZE);

                    var entry = DirectoryEntry.FromBytes(entryBytes);
                    if (entry != null)
                    {
                        result.Add(new DirEntryLocation { Entry = entry, ClusterIndex = cluster, EntryIndex = i });
                    }
                }
            }

            return result;
        }

        public DirEntryLocation? FindDirectoryEntry(int startCluster, string name)
        {
            string key = FormatNameTo8Dot3(name);
            var all = ReadDirectory(startCluster);
            foreach (var loc in all)
            {
                if (string.Equals(loc.Entry.Name11.Trim(), key.Trim(), StringComparison.OrdinalIgnoreCase))
                    return loc;
            }
            return null;
        }

        public DirEntryLocation AddDirectoryEntry(int startCluster, DirectoryEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            entry.Name11 = FormatNameTo8Dot3(entry.GetDisplayName()).PadRight(DirectoryEntry.NAME_LEN, ' ');

            var chain = fat.FollowChain(startCluster);
            int targetCluster = -1;
            int freeIndex = -1;

            foreach (int cluster in chain)
            {
                byte[] clusterBytes = disk.ReadCluster(cluster);
                for (int i = 0; i < FsConstants.CLUSTER_SIZE / DirectoryEntry.ENTRY_SIZE; i++)
                {
                    int offset = i * DirectoryEntry.ENTRY_SIZE;
                    if (clusterBytes[offset] == 0x00) 
                    {
                        targetCluster = cluster;
                        freeIndex = i;
                        break;
                    }
                }
                if (targetCluster != -1) break;
            }

            if (targetCluster == -1)
            {
                int newCluster = fat.AllocateChain(1);
                var tailChain = fat.FollowChain(startCluster);
                int tail = tailChain[tailChain.Count - 1];
                fat.SetFatEntry(tail, newCluster);
                fat.SetFatEntry(newCluster, -1);
                fat.FlushFatToDisk();

                targetCluster = newCluster;
                freeIndex = 0;
            }

            byte[] clusterBuf = disk.ReadCluster(targetCluster);
            byte[] entryBytes = entry.ToBytes();
            int writeOffset = freeIndex * DirectoryEntry.ENTRY_SIZE;
            Array.Copy(entryBytes, 0, clusterBuf, writeOffset, DirectoryEntry.ENTRY_SIZE);

            disk.WriteCluster(targetCluster, clusterBuf);

            return new DirEntryLocation { Entry = entry, ClusterIndex = targetCluster, EntryIndex = freeIndex };
        }

        public void RemoveDirectoryEntry(int startCluster, DirEntryLocation location)
        {
            if (location == null) throw new ArgumentNullException(nameof(location));
            int cluster = location.ClusterIndex;
            int idx = location.EntryIndex;

            byte[] clusterBuf = disk.ReadCluster(cluster);
            int offset = idx * DirectoryEntry.ENTRY_SIZE;
            for (int i = 0; i < DirectoryEntry.ENTRY_SIZE; i++)
                clusterBuf[offset + i] = 0x00;

            disk.WriteCluster(cluster, clusterBuf);

            if (location.Entry.FirstCluster != 0)
            {
                fat.FreeChain(location.Entry.FirstCluster);
                fat.FlushFatToDisk();
            }
        }

        public static string FormatNameTo8Dot3(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return new string(' ', DirectoryEntry.NAME_LEN);

            name = name.Trim().ToUpperInvariant();
            string baseName = name;
            string ext = "";

            int dot = name.IndexOf('.');
            if (dot >= 0)
            {
                baseName = name.Substring(0, dot);
                ext = name.Substring(dot + 1);
            }

            baseName = baseName.Length > 8 ? baseName.Substring(0, 8) : baseName;
            ext = ext.Length > 3 ? ext.Substring(0, 3) : ext;

            baseName = baseName.PadRight(8, ' ');
            ext = ext.PadRight(3, ' ');

            return baseName + ext;
        }

        public static string Parse8Dot3Name(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            string b = raw.Substring(0, Math.Min(8, raw.Length)).Trim();
            string e = raw.Length >= 11 ? raw.Substring(8, 3).Trim() : "";
            return string.IsNullOrEmpty(e) ? b : $"{b}.{e}";
        }
    }
}
