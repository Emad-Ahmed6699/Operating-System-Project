using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace fat_file_system_cs
{
    internal class Shell
    {
        private readonly Directory dir;
        private readonly FileSystem fs;

        private string currentPath;
        private int currentCluster;

        public Shell(Directory dir, FileSystem fs)
        {
            this.dir = dir;
            this.fs = fs;
            this.currentPath = "/";
            this.currentCluster = FsConstants.ROOT_DIR_FIRST_CLUSTER;
        }

        public void Run()
        {
            Console.WriteLine("FAT File System Shell");
            Console.WriteLine("Type 'help' for available commands.\n");

            while (true)
            {
                try
                {
                    Console.Write($"H:{currentPath}> ");
                    string? input = Console.ReadLine();
                    
                    if (string.IsNullOrWhiteSpace(input))
                        continue;

                    var tokens = ParseInput(input);
                    if (tokens.Count == 0)
                        continue;

                    string command = tokens[0].ToLower();
                    var args = tokens.Skip(1).ToList();

                    if (!ExecuteCommand(command, args))
                        break; 
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private static List<string> ParseInput(string input)
        {
            var tokens = new List<string>();
            var currentToken = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (currentToken.Length > 0)
                    {
                        tokens.Add(currentToken.ToString());
                        currentToken.Clear();
                    }
                }
                else
                {
                    currentToken.Append(c);
                }
            }

            if (currentToken.Length > 0)
                tokens.Add(currentToken.ToString());

            return tokens;
        }

        private bool ExecuteCommand(string command, List<string> args)
        {
            switch (command)
            {
                case "cd":
                    CmdChangeDirectory(args);
                    break;
                case "cls":
                    CmdClearScreen();
                    break;
                case "dir":
                    CmdListDirectory(args);
                    break;
                case "quit":
                case "exit":
                    return false;
                case "copy":
                    CmdCopyFile(args);
                    break;
                case "del":
                    CmdDeleteFile(args);
                    break;
                case "help":
                    CmdHelp();
                    break;
                case "md":
                case "mkdir":
                    CmdMakeDirectory(args);
                    break;
                case "rd":
                case "rmdir":
                    CmdRemoveDirectory(args);
                    break;
                case "rename":
                case "ren":
                    CmdRenameFile(args);
                    break;
                case "type":
                    CmdTypeFile(args);
                    break;
                case "write":
                    CmdWriteFile(args);
                    break;
                default:
                    Console.WriteLine($"Unknown command: {command}. Type 'help' for available commands.");
                    break;
            }
            return true;
        }

        private void CmdChangeDirectory(List<string> args)
        {
            if (args.Count == 0)
            {
                Console.WriteLine(currentPath);
                return;
            }

            string targetPath = args[0];

            if (targetPath == "/")
            {
                currentPath = "/";
                currentCluster = FsConstants.ROOT_DIR_FIRST_CLUSTER;
                return;
            }

            if (targetPath == "..")
            {
                if (currentPath == "/")
                {
                    Console.WriteLine("Already at root directory.");
                    return;
                }

                int lastSlash = currentPath.LastIndexOf('/');
                if (lastSlash == 0)
                {
                    currentPath = "/";
                    currentCluster = FsConstants.ROOT_DIR_FIRST_CLUSTER;
                }
                else
                {
                    currentPath = currentPath.Substring(0, lastSlash);
                    currentCluster = ResolvePath(currentPath);
                }
                return;
            }

            var entryLoc = dir.FindDirectoryEntry(currentCluster, targetPath);
            if (entryLoc == null)
            {
                Console.WriteLine($"Directory not found: {targetPath}");
                return;
            }

            if (entryLoc.Entry.Attribute != FsConstants.ATTR_DIRECTORY)
            {
                Console.WriteLine($"'{targetPath}' is not a directory.");
                return;
            }

            currentCluster = entryLoc.Entry.FirstCluster;
            currentPath = currentPath == "/" ? $"/{targetPath}" : $"{currentPath}/{targetPath}";
        }

        private int ResolvePath(string path)
        {
            if (path == "/")
                return FsConstants.ROOT_DIR_FIRST_CLUSTER;

            var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            int cluster = FsConstants.ROOT_DIR_FIRST_CLUSTER;

            foreach (var part in parts)
            {
                var entryLoc = dir.FindDirectoryEntry(cluster, part);
                if (entryLoc == null || entryLoc.Entry.Attribute != FsConstants.ATTR_DIRECTORY)
                    throw new DirectoryNotFoundException($"Path not found: {path}");
                cluster = entryLoc.Entry.FirstCluster;
            }

            return cluster;
        }

        private static void CmdClearScreen()
        {
            Console.Clear();
        }

        private void CmdListDirectory(List<string> args)
        {
            int targetCluster = currentCluster;

            if (args.Count > 0)
            {
                var entryLoc = dir.FindDirectoryEntry(currentCluster, args[0]);
                if (entryLoc == null)
                {
                    Console.WriteLine($"Directory not found: {args[0]}");
                    return;
                }
                if (entryLoc.Entry.Attribute != FsConstants.ATTR_DIRECTORY)
                {
                    Console.WriteLine($"'{args[0]}' is not a directory.");
                    return;
                }
                targetCluster = entryLoc.Entry.FirstCluster;
            }

            var entries = dir.ReadDirectory(targetCluster);
            
            Console.WriteLine($"\nDirectory of {currentPath}\n");
            Console.WriteLine($"{"Name",-15} {"Type",-10} {"Size",-10} {"Cluster",-10}");
            Console.WriteLine(new string('-', 50));

            foreach (var entry in entries.Select(entryLoc => entryLoc.Entry))
            {
                string name = FormatNameForDisplay(entry.Name11);
                string type = entry.Attribute == FsConstants.ATTR_DIRECTORY ? "<DIR>" : "FILE";
                string size = entry.Attribute == FsConstants.ATTR_DIRECTORY ? "" : entry.FileSize.ToString();
                
                Console.WriteLine($"{name,-15} {type,-10} {size,-10} {entry.FirstCluster,-10}");
            }

            Console.WriteLine($"\n{entries.Count} item(s)");
        }

        private static string FormatNameForDisplay(string name)
        {
            string baseName = name.Substring(0, 8).Trim();
            string ext = name.Substring(8, 3).Trim();
            return string.IsNullOrEmpty(ext) ? baseName : $"{baseName}.{ext}";
        }

        private void CmdCopyFile(List<string> args)
        {
            if (args.Count < 2)
            {
                Console.WriteLine("Usage: copy <source> <destination>");
                return;
            }

            string src = args[0];
            string dst = args[1];

            try
            {
                fs.CopyFile(currentCluster, src, currentCluster, dst);
                Console.WriteLine($"Copied {src} to {dst}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Copy failed: {ex.Message}");
            }
        }

        private void CmdDeleteFile(List<string> args)
        {
            if (args.Count == 0)
            {
                Console.WriteLine("Usage: del <filename>");
                return;
            }

            string filename = args[0];

            try
            {
                fs.DeleteFile(currentCluster, filename);
                Console.WriteLine($"Deleted {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Delete failed: {ex.Message}");
            }
        }

        private static void CmdHelp()
        {
            Console.WriteLine("\nAvailable Commands:");
            Console.WriteLine("  cd [dir]              - Change directory or show current directory");
            Console.WriteLine("  cls                   - Clear the screen");
            Console.WriteLine("  dir [dir]             - List directory contents");
            Console.WriteLine("  quit                  - Exit the shell");
            Console.WriteLine("  copy <src> <dst>      - Copy a file");
            Console.WriteLine("  del <file>            - Delete a file");
            Console.WriteLine("  help                  - Show this help message");
            Console.WriteLine("  md <dir>              - Create a directory");
            Console.WriteLine("  rd <dir>              - Remove an empty directory");
            Console.WriteLine("  rename <old> <new>    - Rename a file or directory");
            Console.WriteLine("  type <file>           - Display file contents");
            Console.WriteLine("  write <file>          - Write content to file");
            Console.WriteLine("\nNote: All filenames must follow 8.3 format (e.g., FILENAME.TXT)");
        }

        private void CmdMakeDirectory(List<string> args)
        {
            if (args.Count == 0)
            {
                Console.WriteLine("Usage: md <directory>");
                return;
            }

            string dirname = args[0];

            try
            {
                fs.CreateDirectory(currentCluster, dirname);
                Console.WriteLine($"Directory created: {dirname}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Create directory failed: {ex.Message}");
            }
        }

        private void CmdRemoveDirectory(List<string> args)
        {
            if (args.Count == 0)
            {
                Console.WriteLine("Usage: rd <directory>");
                return;
            }

            string dirname = args[0];

            try
            {
                fs.RemoveDirectory(currentCluster, dirname);
                Console.WriteLine($"Directory removed: {dirname}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Remove directory failed: {ex.Message}");
            }
        }

        private void CmdRenameFile(List<string> args)
        {
            if (args.Count < 2)
            {
                Console.WriteLine("Usage: rename <oldname> <newname>");
                return;
            }

            string oldName = args[0];
            string newName = args[1];

            try
            {
                fs.RenameEntry(currentCluster, oldName, newName);
                Console.WriteLine($"Renamed {oldName} to {newName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Rename failed: {ex.Message}");
            }
        }

        private void CmdTypeFile(List<string> args)
        {
            if (args.Count == 0)
            {
                Console.WriteLine("Usage: type <filename>");
                return;
            }

            string filename = args[0];

            try
            {
                string content = fs.ReadFile(currentCluster, filename);
                Console.WriteLine(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Type failed: {ex.Message}");
            }
        }

        private void CmdWriteFile(List<string> args)
        {
            if (args.Count == 0)
            {
                Console.WriteLine("Usage: write <filename>");
                return;
            }

            string filename = args[0];

            try
            {
                Console.Write("Enter content: ");
                string? content = Console.ReadLine();
                
                if (content == null)
                {
                    Console.WriteLine("Write cancelled.");
                    return;
                }

                // Check if file exists, if not create it
                if (dir.FindDirectoryEntry(currentCluster, filename) == null)
                {
                    fs.CreateFile(currentCluster, filename);
                }

                fs.WriteFile(currentCluster, filename, content);
                Console.WriteLine($"File written: {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Write failed: {ex.Message}");
            }
        }
    }
}
