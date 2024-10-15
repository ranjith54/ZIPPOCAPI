using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace ZIPPOCAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ZipController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Constructor to inject the HttpClient for downloading files.
        /// </summary>
        /// <param name="httpClient">Injected HttpClient instance.</param>
        public ZipController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Downloads multiple files and folders, compresses them into a ZIP file, and returns the ZIP for download.
        /// </summary>
        /// <param name="downloadZip">An object containing the list of items to be included in the ZIP and the desired name of the ZIP file.</param>
        /// <returns>A ZIP file containing the requested items, or a BadRequest response if the input is invalid.</returns>
        [HttpPost("download-zip")]
        public async Task<IActionResult> DownloadZip(DownloadZip downloadZip)
        {
            // Validate input parameters
            if (downloadZip == null || downloadZip.Items == null || string.IsNullOrWhiteSpace(downloadZip.Name))
            {
                return BadRequest("No items provided or invalid zip file name.");
            }

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                // Recursively process the list of items and add them to the ZIP archive
                await ProcessZipItemsAsync(archive, downloadZip.Items);
            }

            memoryStream.Seek(0, SeekOrigin.Begin); // Reset the memory stream position

            // Return the ZIP file as a downloadable file
            return File(memoryStream.ToArray(), "application/zip", $"{downloadZip.Name}.zip");
        }


        /// <summary>
        /// Recursively processes the list of items, adding files and folders to the ZIP archive.
        /// </summary>
        /// <param name="archive">The ZIP archive where items will be added.</param>
        /// <param name="items">List of items to process, which can be files or folders.</param>
        /// <param name="parentFolderPath">The folder path in the ZIP where items will be added. Defaults to an empty string for the root level.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ProcessZipItemsAsync(ZipArchive archive, List<ZipItem> items, string parentFolderPath = "")
        {
            foreach (var item in items)
            {
                // Handle folders
                if (item.Type == ItemType.Folder)
                {
                    // Create the folder entry in the ZIP archive
                    string folderPath = Path.Combine(parentFolderPath, item.Name)
                        .Replace("\\", "/")
                        .TrimEnd('/') + "/";  // Ensure folder ends with a slash
                    archive.CreateEntry(folderPath);

                    // Recursively process the contents of the folder
                    if (item.Items != null && item.Items.Count > 0)
                    {
                        await ProcessZipItemsAsync(archive, item.Items, folderPath);  // Recursion with updated folder path
                    }
                }
                // Handle files
                else if (item.Type == ItemType.File)
                {
                    byte[]? fileContent = null;
                    try
                    {
                        // Download the file content from the provided URL
                        fileContent = await _httpClient.GetByteArrayAsync(item.Url);
                    }
                    catch (HttpRequestException ex)
                    {
                        // Log the error and skip the file if download fails
                        //_logger.LogError(ex, $"Failed to download file: {item.Url}");
                        continue;
                    }

                    if (fileContent != null)
                    {
                        // Create the file entry in the ZIP archive
                        string filePath = Path.Combine(parentFolderPath, item.Name).Replace("\\", "/");
                        var zipEntry = archive.CreateEntry(filePath, CompressionLevel.Fastest);

                        // Write the file content into the ZIP entry
                        using var entryStream = zipEntry.Open();
                        await entryStream.WriteAsync(fileContent);
                    }
                }
            }
        }
    }
}
