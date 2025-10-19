using System.IO;
using System.IO.Pipes;

namespace fat_file_system_cs
{
    internal class VirtualDisk
    {
        private const int CLUSTER_SIZE = 1024;
        private const int CLUSTERS_NUMBER = 1024;
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
            this.diskSize = CLUSTERS_NUMBER * CLUSTER_SIZE;

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

                byte[] emptyClusterPlaceholder = new byte[CLUSTER_SIZE];
                for (int i = 0; i < CLUSTERS_NUMBER; i++)
                {
                    tempFileStream.Write(emptyClusterPlaceholder, 0, CLUSTER_SIZE);
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
    }
}
