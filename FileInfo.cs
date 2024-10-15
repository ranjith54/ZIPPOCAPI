namespace ZIPPOCAPI
{
    /// <summary>
    /// Represents a file or folder to be added to a ZIP archive.
    /// </summary>
    public class ZipItem
    {
        /// <summary>
        /// The name of the file or folder.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The URL to download the file content from (only for files).
        /// </summary>
        public string? Url { get; set; } // For files only

        /// <summary>
        /// Specifies whether the item is a file or folder.
        /// </summary>
        public ItemType Type { get; set; }

        /// <summary>
        /// List of nested ZipItems (only for folders).
        /// </summary>
        public List<ZipItem>? Items { get; set; }  // For folders only
    }

    /// <summary>
    /// Enum to represent the type of item (File or Folder).
    /// </summary>
    public enum ItemType
    {
        File = 0,
        Folder = 1
    }

    /// <summary>
    /// Represents a request for downloading a ZIP file that contains multiple items.
    /// </summary>
    public class DownloadZip
    {
        /// <summary>
        /// Gets or sets the list of items (files and folders) to be included in the ZIP file.
        /// </summary>
        public List<ZipItem> Items { get; set; }

        /// <summary>
        /// Gets or sets the name of the resulting ZIP file.
        /// </summary>
        public string Name { get; set; }
    }

}
