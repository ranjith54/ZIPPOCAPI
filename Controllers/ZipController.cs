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
        /// Constructor for ZipController.
        /// Initializes the HttpClient used for downloading files.
        /// </summary>
        /// <param name="httpClient">Injected HttpClient instance for making HTTP requests.</param>
        public ZipController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Downloads multiple items (files and folders) and compresses them into a ZIP archive.
        /// </summary>
        /// <param name="items">A list of ZipItem objects, which can be files or folders.</param>
        /// <param name="zipFileName">The name of the resulting ZIP file.</param>
        /// <returns>An IActionResult containing the ZIP file for download.</returns>
        [HttpPost("download-multiple-items")]
        public async Task<IActionResult> DownloadMultipleItems(List<ZipItem> items, string zipFileName)
        {
            // Check if items are provided and if zipFileName is valid
            if (items == null || items.Count == 0 || string.IsNullOrWhiteSpace(zipFileName))
            {
                return BadRequest("No items provided or invalid zip file name.");
            }

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                // Create nested folders based on the folderPath and folder name
                foreach (var folder in items.Where(item => item.Type == ItemType.Folder))
                {
                    // Construct the full folder path
                    string fullFolderPath = Path.Combine(folder.FolderPath, folder.Name)
                        .Replace("\\", "/")  // Ensure paths are formatted correctly for ZIP
                        .TrimEnd('/') + "/"; // Ensure folder path ends with a slash

                    // Create the folder in the ZIP archive
                    archive.CreateEntry(fullFolderPath);
                }

                // Create tasks to download each file in parallel
                var downloadTasks = items.Where(item => item.Type == ItemType.File)
                    .Select(async item =>
                    {
                        byte[]? fileContent = null;
                        try
                        {
                            // Attempt to download the file
                            fileContent = await _httpClient.GetByteArrayAsync(item.Url);
                        }
                        catch (HttpRequestException ex)
                        {
                            // Log the error if necessary
                            //_logger.LogError(ex, $"Failed to download file: {item.Url}");
                        }

                        // Return file information including the content, name, and folder path
                        return new { item.Url, fileContent, item.Name, item.FolderPath };
                    }).ToList();

                // Wait for all download tasks to complete
                var downloadedFiles = await Task.WhenAll(downloadTasks);

                // Process each successfully downloaded file and add it to the ZIP
                foreach (var file in downloadedFiles)
                {
                    if (file.fileContent != null) // Ensure the file was downloaded successfully
                    {
                        var fileName = file.Name;
                        var folderPath = string.IsNullOrEmpty(file.FolderPath) ? "" : file.FolderPath.TrimEnd('/') + "/";

                        // Ensure the full folder path for the file is nested properly
                        var fullPath = $"{folderPath}{fileName}".Replace("\\", "/");

                        // Create a ZIP entry for the downloaded file, including the folder path
                        var zipEntry = archive.CreateEntry(fullPath, CompressionLevel.Fastest);

                        // Write the downloaded file into the ZIP entry
                        using var entryStream = zipEntry.Open();
                        await entryStream.WriteAsync(file.fileContent);
                    }
                }
            }

            memoryStream.Seek(0, SeekOrigin.Begin); // Reset the memory stream position

            // Return the ZIP file as a downloadable file
            return File(memoryStream.ToArray(), "application/zip", $"{zipFileName}.zip");
        }



    }
}
