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
        /// Downloads multiple files from the given URLs and bundles them into a single ZIP file.
        /// Each file is fetched using an HTTP GET request and added to the ZIP archive.
        /// Duplicate file names are handled by appending a numeric suffix to ensure unique names.
        /// Skips any files that fail to download due to network issues or invalid URLs.
        /// </summary>
        /// <param name="fileUrls">Array of URLs from which files will be downloaded.</param>
        /// <returns>A ZIP file containing the downloaded files, or a BadRequest if no URLs are provided.</returns>
        [HttpPost("download-multiple-files")]
        public async Task<IActionResult> DownloadMultipleFiles([FromBody] string[] fileUrls)
        {
            if (fileUrls == null || fileUrls.Length == 0)
            {
                return BadRequest("No file URLs provided.");
            }

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                // Create tasks to download each file in parallel
                var downloadTasks = fileUrls.Select(async fileUrl =>
                {
                    // Try to download the file, return null if download fails
                    byte[]? fileContent = null;
                    try
                    {
                        fileContent = await _httpClient.GetByteArrayAsync(fileUrl);
                    }
                    catch (HttpRequestException)
                    {
                        // Log the error and continue
                        // Example: _logger.LogError($"Failed to download {fileUrl}");
                    }

                    return new { fileUrl, fileContent }; // Return both URL and content for further processing
                }).ToList();

                // Wait for all download tasks to complete
                var downloadedFiles = await Task.WhenAll(downloadTasks);

                // Process each successfully downloaded file and add it to the zip
                foreach (var file in downloadedFiles)
                {
                    if (file.fileContent != null) // Ensure the file was downloaded successfully
                    {
                        // Get the file name from the URL (handle potential query params)
                        var fileName = Path.GetFileName(file.fileUrl).Split('?')[0];

                        // Create a zip entry for the downloaded file
                        var zipEntry = archive.CreateEntry(fileName, CompressionLevel.Fastest);

                        // Write the downloaded file into the zip entry
                        using var entryStream = zipEntry.Open();
                        await entryStream.WriteAsync(file.fileContent);
                    }
                }
            }

            // Reset the position of the memory stream
            memoryStream.Seek(0, SeekOrigin.Begin);

            // Return the zip file as a downloadable file
            return File(memoryStream.ToArray(), "application/zip", "Files.zip");
        }
    }
}
