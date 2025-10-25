using System.IO;
using System.IO.Pipes;

namespace fat_file_system_cs
{
    internal class VirtualDisk
    {
        /* • In our implementation:
              ⚬ Total clusters: 1,024
              ⚬ Cluster size: 1,024 bytes
        */

        private long diskSize = 0;
        private string? diskPath = null;
        private FileStream? diskFileStream = null;
        private bool isOpen = false;

        /// <summary>
        /// Initializes the virtual disk.
        /// - If the disk file exists, it opens it for read/write access.
        /// - If it does not exist and <paramref name="createIfMissing"/> is true, it creates a new empty virtual disk file.
        /// - Throws an exception if the disk is already initialized or if the file cannot be opened/created.
        /// </summary>
        /// <param name="path">The file path of the virtual disk.</param>
        /// <param name="createIfMissing">If true, creates the disk file if it does not exist.</param>
        /// <exception cref="InvalidOperationException">Thrown if the disk is already initialized.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the disk file does not exist and creation is disabled.</exception>
        /// <exception cref="IOException">Thrown for general I/O errors during initialization.</exception>
        public void Initialize(string path, bool createIfMissing = true)
        {
            if (this.isOpen)
            {
                throw new InvalidOperationException("Disk is already initialized");
            }

            this.diskPath = path;
            this.diskSize = FsConstants.CLUSTER_COUNT * FsConstants.CLUSTER_SIZE;

            try
            {
                if (!File.Exists(diskPath))
                {
                    if (createIfMissing)
                    {
                        this.CreateEmptyDisk(path);
                    }
                    else
                    {
                        throw new FileNotFoundException("Couldn't find the specified disk path");
                    }
                }

                this.diskFileStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                this.isOpen = true;
            }
            catch (Exception ex)
            {
                this.isOpen = false;
                throw new IOException($"Failed to open disk: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates a new empty virtual disk file.
        /// - The file is filled with zeroed clusters, each of size <see cref="CLUSTER_SIZE"/>.
        /// - The total file size equals <see cref="CLUSTERS_NUMBER"/> × <see cref="CLUSTER_SIZE"/>.
        /// - Ensures the disk structure is properly initialized before use.
        /// </summary>
        /// <param name="path">The path where the disk file should be created.</param>
        /// <exception cref="IOException">Thrown if disk creation fails.</exception>
        private void CreateEmptyDisk(string path)
        {
            FileStream? tempFileStream = null;

            try
            {
                tempFileStream = new FileStream(path, FileMode.Create, FileAccess.Write);

                byte[] emptyClusterPlaceholder = new byte[FsConstants.CLUSTER_SIZE];
                for (int i = 0; i < FsConstants.CLUSTER_COUNT; i++)
                {
                    tempFileStream.Write(emptyClusterPlaceholder, 0, FsConstants.CLUSTER_SIZE);
                }

                tempFileStream.Flush();
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to create disk file: {ex.Message}", ex);
            }
            finally
            {
                if (tempFileStream != null)
                {
                    tempFileStream.Close();
                }
            }
        }
        public byte[] ReadCluster(int clusterNumber)
        {
            if (!isOpen) throw new InvalidOperationException("Disk not initialized");
            if (clusterNumber < 0 || clusterNumber >= FsConstants.CLUSTER_COUNT)
                throw new ArgumentOutOfRangeException(nameof(clusterNumber));

            byte[] buffer = new byte[FsConstants.CLUSTER_SIZE];
            diskFileStream!.Seek(clusterNumber * FsConstants.CLUSTER_SIZE, SeekOrigin.Begin);
            int bytesRead = diskFileStream.Read(buffer, 0, FsConstants.CLUSTER_SIZE);

            if (bytesRead < FsConstants.CLUSTER_SIZE)
                throw new IOException("Incomplete cluster read");

            return buffer;
        }
        public void WriteCluster(int clusterNumber, byte[] data)
        {
            if (!isOpen) throw new InvalidOperationException("Disk not initialized");
            if (clusterNumber < 0 || clusterNumber >= FsConstants.CLUSTER_COUNT)
                throw new ArgumentOutOfRangeException(nameof(clusterNumber));
            if (data.Length != FsConstants.CLUSTER_SIZE)
                throw new ArgumentException($"Data must be exactly {FsConstants.CLUSTER_SIZE} bytes.");

            diskFileStream!.Seek(clusterNumber * FsConstants.CLUSTER_SIZE, SeekOrigin.Begin);
            diskFileStream.Write(data, 0, FsConstants.CLUSTER_SIZE);
            diskFileStream.Flush();
        }
        public long GetDiskSize()
        {
            return diskSize;
        }

        public void CloseDisk()
        {
            if (diskFileStream != null)
            {
                diskFileStream.Flush();
                diskFileStream.Close();
                diskFileStream = null;
            }
            isOpen = false;
        }
    }
}
