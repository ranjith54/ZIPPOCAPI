namespace ZIPPOCAPI
{
    /// <summary>
    /// Enum to identify the type of item (File or Folder).
    /// </summary>
    public enum ItemType
    {
        File,
        Folder
    }

    /// <summary>
    /// Represents an item to be added to the ZIP archive. This can either be a file or a folder.
    /// </summary>
    public class ZipItem
    {
        /// <summary>
        /// The name of the file or folder.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The URL for downloading the file. This will be null for folders.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// The folder path where the file or folder should be placed in the ZIP archive.
        /// </summary>
        public string FolderPath { get; set; }

        /// <summary>
        /// Indicates whether this item is a file or a folder.
        /// </summary>
        public ItemType Type { get; set; } // Whether this is a file or a folder
    }
}
