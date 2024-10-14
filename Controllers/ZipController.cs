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
        /// Downloads multiple items (files or folders), creates a ZIP archive, and returns it as a downloadable file.
        /// </summary>
        /// <param name="items">A list of items (files and folders) to include in the ZIP file.</param>
        /// <param name="zipFileName">The name of the resulting ZIP file (without the .zip extension).</param>
        /// <returns>A downloadable ZIP file containing the specified items.</returns>
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
                // Process folder creation first
                foreach (var folder in items.Where(item => item.Type == ItemType.Folder))
                {
                    // Create nested folders based on the folderPath and folder name
                    string fullFolderPath = Path.Combine(folder.FolderPath, folder.Name).Replace("\\", "/").TrimEnd('/') + "/";

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
                                                 // Log the error here if necessary
                                                 //_logger.LogError(ex, $"Failed to download file: {item.Url}");
                                             }

                                             return new { item.Url, fileContent, item.Name, item.FolderPath };
                                         }).ToList();

                // Wait for all download tasks to complete
                var downloadedFiles = await Task.WhenAll(downloadTasks);

                // Process each successfully downloaded file and add it to the zip
                foreach (var file in downloadedFiles)
                {
                    if (file.fileContent != null)
                    {
                        var fileName = file.Name;
                        var folderPath = string.IsNullOrEmpty(file.FolderPath) ? "" : file.FolderPath.TrimEnd('/') + "/";

                        // Ensure the full folder path for the file is nested properly
                        var fullPath = $"{folderPath}{fileName}".Replace("\\", "/");

                        // Create a zip entry for the downloaded file, including the folder path
                        var zipEntry = archive.CreateEntry(fullPath, CompressionLevel.Fastest);

                        // Write the downloaded file into the zip entry
                        using var entryStream = zipEntry.Open();
                        await entryStream.WriteAsync(file.fileContent);
                    }
                }
            }

            memoryStream.Seek(0, SeekOrigin.Begin);

            // Return the zip file as a downloadable file
            return File(memoryStream.ToArray(), "application/zip", $"{zipFileName}.zip");
        }


    }
}
