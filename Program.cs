using System;
using System.Text;

namespace fat_file_system_cs
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //string diskPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"C:\Users\AIO\OneDrive\Desktop\OS\section\1\minifat.bin");

            //VirtualDisk vd = new VirtualDisk(diskPath);

            try
            {
                // TODO: Test the virtual disk then clean up the main the virtual disk will not be created here at the end}
                VirtualDisk disk = new VirtualDisk();
                disk.Initialize("minifat.bin");
                byte[] data = new byte[1024];
                data[0] = 65; // 'A'
                disk.WriteCluster(5, data);
                disk.CloseDisk();

                // Reopen
                disk.Initialize("minifat.bin", false);
                byte[] readBack = disk.ReadCluster(5);
                Console.WriteLine("=== Cluster 5 contents (decimal) ===");
                for (int i = 0; i < readBack.Length; i++)
                {
                    Console.Write(readBack[i].ToString().PadLeft(3, ' ') + " ");
                    if ((i + 1) % 16 == 0)
                        Console.WriteLine();
                }

                Console.WriteLine("\n\n=== Cluster 5 contents (hexadecimal) ===");
                for (int i = 0; i < readBack.Length; i++)
                {
                    Console.Write(readBack[i].ToString("X2") + " ");
                    if ((i + 1) % 16 == 0)
                        Console.WriteLine(); 
                }
                disk.CloseDisk();
            }
            catch (Exception ex) {

                Console.WriteLine("Error: " + ex.Message);
            }

            //example on task 4
                try
                {
                    VirtualDisk disk = new VirtualDisk();
                    disk.Initialize("minifat.bin", true);

                    FatTableManager fat = new FatTableManager(disk);
                    fat.LoadFatFromDisk();

                    DirectoryManager dm = new DirectoryManager(disk, fat);

                    int root = FsConstants.ROOT_DIR_FIRST_CLUSTER;

                    Console.WriteLine("Root before add:");
                    var list1 = dm.ReadDirectory(root);
                    foreach (var e in list1) Console.WriteLine($"{e.Entry.GetDisplayName()} cl={e.Entry.FirstCluster} size={e.Entry.FileSize}");

                    DirectoryEntry newEntry = new DirectoryEntry()
                    {
                        Name11 = DirectoryManager.FormatNameTo8Dot3("hello.txt"),
                        Attribute = 0x01,
                        FirstCluster = fat.AllocateChain(2),
                        FileSize = 2048
                    };
                    fat.FlushFatToDisk();
                    var loc = dm.AddDirectoryEntry(root, newEntry);

                    Console.WriteLine("\nRoot after add:");
                    var list2 = dm.ReadDirectory(root);
                    foreach (var e in list2) Console.WriteLine($"{e.Entry.GetDisplayName()} cl={e.Entry.FirstCluster} size={e.Entry.FileSize}");

                    var found = dm.FindDirectoryEntry(root, "hello.txt");
                    if (found != null)
                    {
                        dm.RemoveDirectoryEntry(root, found);
                    }

                    Console.WriteLine("\nRoot after remove:");
                    var list3 = dm.ReadDirectory(root);
                    foreach (var e in list3) Console.WriteLine($"{e.Entry.GetDisplayName()} cl={e.Entry.FirstCluster} size={e.Entry.FileSize}");

                    disk.CloseDisk();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
    }
}
