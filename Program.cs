using System;
using System.Text;

namespace fat_file_system_cs
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string diskPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\virtual_disk.bin");

            VirtualDisk vd = new VirtualDisk(diskPath);

            try
            {   
                // TODO: Test the virtual disk then clean up the main the virtual disk will not be created here at the end}
            }
            catch (Exception ex){ }
}
