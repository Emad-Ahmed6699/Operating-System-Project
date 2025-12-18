using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fat_file_system_cs
{
    internal class FileSystem
    {
        private readonly VirtualDisk disk;
        private readonly FatTableManager fat;
        private readonly Directory dir;

        public FileSystem(VirtualDisk disk, FatTableManager fat, Directory dir)
        {
            this.disk = disk;
            this.fat = fat;
            this.dir = dir;
        }

        // ================= FILE OPERATIONS =================

        public void CreateFile(int parentCluster, string fileName)
        {
            if (dir.FindDirectoryEntry(parentCluster, fileName) != null)
                throw new IOException("File already exists");

            DirectoryEntry entry = new DirectoryEntry
            {
                Name11 = Directory.FormatNameTo8Dot3(fileName),
                Attribute = 0x20,   // file (regular file attribute)
                FirstCluster = 0,
                FileSize = 0
            };

            dir.AddDirectoryEntry(parentCluster, entry);
        }

        public void WriteFile(int parentCluster, string fileName, string content)
        {
            var loc = dir.FindDirectoryEntry(parentCluster, fileName)
                      ?? throw new FileNotFoundException("File not found");

            DirectoryEntry entry = loc.Entry;

            byte[] data = Converter.StringToBytes(content);
            int neededClusters =
                (data.Length + FsConstants.CLUSTER_SIZE - 1) / FsConstants.CLUSTER_SIZE;

            if (entry.FirstCluster != 0)
                fat.FreeChain(entry.FirstCluster);

            if (neededClusters > 0)
            {
                int start = fat.AllocateChain(neededClusters);
                entry.FirstCluster = start;

                var chain = fat.FollowChain(start);
                int offset = 0;

                foreach (int c in chain)
                {
                    byte[] buf = new byte[FsConstants.CLUSTER_SIZE];
                    int n = Math.Min(FsConstants.CLUSTER_SIZE, data.Length - offset);
                    Buffer.BlockCopy(data, offset, buf, 0, n);
                    disk.WriteCluster(c, buf);
                    offset += n;
                }
            }

            entry.FileSize = data.Length;
            dir.RemoveDirectoryEntry(parentCluster, loc);
            dir.AddDirectoryEntry(parentCluster, entry);

            fat.FlushFatToDisk();
        }

        public string ReadFile(int parentCluster, string fileName)
        {
            var loc = dir.FindDirectoryEntry(parentCluster, fileName)
                      ?? throw new FileNotFoundException("File not found");

            DirectoryEntry entry = loc.Entry;

            if (entry.FirstCluster == 0)
                return string.Empty;

            byte[] result = new byte[entry.FileSize];
            int offset = 0;

            foreach (int c in fat.FollowChain(entry.FirstCluster))
            {
                byte[] cluster = disk.ReadCluster(c);
                int n = Math.Min(FsConstants.CLUSTER_SIZE, entry.FileSize - offset);
                Buffer.BlockCopy(cluster, 0, result, offset, n);
                offset += n;
                if (offset >= entry.FileSize) break;
            }

            return Converter.BytesToString(result);
        }

        public void DeleteFile(int parentCluster, string fileName)
        {
            var loc = dir.FindDirectoryEntry(parentCluster, fileName)
                      ?? throw new FileNotFoundException("File not found");

            if (loc.Entry.FirstCluster != 0)
                fat.FreeChain(loc.Entry.FirstCluster);

            dir.RemoveDirectoryEntry(parentCluster, loc);
            fat.FlushFatToDisk();
        }

        public void CopyFile(int sourceParentCluster, string sourceFileName, int destParentCluster, string destFileName)
        {
            var sourceLoc = dir.FindDirectoryEntry(sourceParentCluster, sourceFileName)
                           ?? throw new FileNotFoundException("Source file not found");

            if (sourceLoc.Entry.Attribute == FsConstants.ATTR_DIRECTORY)
                throw new IOException("Cannot copy a directory");

            // Read the source file content
            string content = ReadFile(sourceParentCluster, sourceFileName);

            // Create the destination file and write content
            CreateFile(destParentCluster, destFileName);
            WriteFile(destParentCluster, destFileName, content);
        }

        public void RenameEntry(int parentCluster, string oldName, string newName)
        {
            var loc = dir.FindDirectoryEntry(parentCluster, oldName)
                      ?? throw new FileNotFoundException("File or directory not found");

            DirectoryEntry entry = loc.Entry;
            entry.Name11 = Directory.FormatNameTo8Dot3(newName);

            dir.RemoveDirectoryEntry(parentCluster, loc);
            dir.AddDirectoryEntry(parentCluster, entry);

            fat.FlushFatToDisk();
        }

        // ================= DIRECTORY OPERATIONS =================

        public void CreateDirectory(int parentCluster, string name)
        {
            if (dir.FindDirectoryEntry(parentCluster, name) != null)
                throw new IOException("Directory already exists");

            int start = fat.AllocateChain(1);
            disk.WriteCluster(start, new byte[FsConstants.CLUSTER_SIZE]);

            DirectoryEntry entry = new DirectoryEntry
            {
                Name11 = Directory.FormatNameTo8Dot3(name),
                Attribute = FsConstants.ATTR_DIRECTORY,   // directory (0x10)
                FirstCluster = start,
                FileSize = 0
            };

            dir.AddDirectoryEntry(parentCluster, entry);
            fat.FlushFatToDisk();
        }

        public void RemoveDirectory(int parentCluster, string name)
        {
            var loc = dir.FindDirectoryEntry(parentCluster, name)
                      ?? throw new FileNotFoundException("Directory not found");

            if (dir.ReadDirectory(loc.Entry.FirstCluster).Count != 0)
                throw new IOException("Directory not empty");

            fat.FreeChain(loc.Entry.FirstCluster);
            dir.RemoveDirectoryEntry(parentCluster, loc);
            fat.FlushFatToDisk();
        }
        public void ListDirectorySimple(int clusterNumber)
        {
            var entries = dir.ReadDirectory(clusterNumber);

            Console.WriteLine($"\nعدد العناصر: {entries.Count}");

            foreach (var loc in entries.Select(loc => loc.Entry))
            {
                string name = loc.GetDisplayName();
                bool isDir = (loc.Attribute & FsConstants.ATTR_DIRECTORY) != 0;

                if (isDir)
                    Console.WriteLine($"📁 {name}");
                else
                    Console.WriteLine($"📄 {name} ({loc.FileSize} bytes)");
            }
            Console.WriteLine();
        }

        // Compatibility wrapper for shell
        public void ListDirectory(int clusterNumber)
        {
            ListDirectorySimple(clusterNumber);
        }

    }
}
