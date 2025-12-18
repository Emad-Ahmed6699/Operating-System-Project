using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fat_file_system_cs
{
    internal class Shell
    {
        private readonly FileSystem fs;
        private int currentCluster = FsConstants.ROOT_DIR_FIRST_CLUSTER;

        public Shell(FileSystem fs)
        {
            this.fs = fs;
        }

        public void Run()
        {
            Console.WriteLine("Mini FAT File System");
            Console.WriteLine("Type 'help' for commands\n");

            while (true)
            {
                Console.Write("FS> ");
                string input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                var parts = input.Split(' ', 2);
                string cmd = parts[0].ToLower();
                string arg = parts.Length > 1 ? parts[1] : "";

                try
                {
                    switch (cmd)
                    {
                        case "help":
                            ShowHelp();
                            break;

                        case "dir":
                            fs.ListDirectory(currentCluster);
                            break;

                        case "md":
                            fs.CreateDirectory(currentCluster, arg);
                            break;

                        case "rd":
                            fs.RemoveDirectory(currentCluster, arg);
                            break;

                        case "type":
                            Console.WriteLine(fs.ReadFile(currentCluster, arg));
                            break;

                        case "write":
                            Console.Write("Enter content: ");
                            string content = Console.ReadLine();
                            fs.WriteFile(currentCluster, arg, content);
                            break;

                        case "del":
                            fs.DeleteFile(currentCluster, arg);
                            break;

                        case "exit":
                            return;

                        default:
                            Console.WriteLine("Unknown command");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }

        private void ShowHelp()
        {
            Console.WriteLine("\nCommands:");
            Console.WriteLine(" dir               - list directory");
            Console.WriteLine(" md <name>         - create directory");
            Console.WriteLine(" rd <name>         - remove directory");
            Console.WriteLine(" write <file>      - write file");
            Console.WriteLine(" type <file>       - read file");
            Console.WriteLine(" del <file>        - delete file");
            Console.WriteLine(" exit              - quit\n");
        }
    }
}
