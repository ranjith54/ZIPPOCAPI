using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace ZIPPOCAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ZipController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public ZipController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpPost("download-multiple-files")]
        public async Task<IActionResult> DownloadMultipleFiles([FromBody] string[] fileUrls)
        {
            if (fileUrls == null || fileUrls.Length == 0)
            {
                return BadRequest("No file URLs provided.");
            }

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var fileUrl in fileUrls)
                    {
                        // Try to download the file, skip if download fails
                        byte[] fileContent;
                        try
                        {
                            fileContent = await _httpClient.GetByteArrayAsync(fileUrl);
                        }
                        catch (HttpRequestException ex)
                        {
                            // Handle the error, e.g., log and continue with other files
                            continue;  // Skip this file if download fails
                        }

                        // Get the file name from the URL (or generate a custom name if needed)
                        var fileName = Path.GetFileName(fileUrl).Split('?')[0];                       

                        // Create a zip entry for each downloaded file
                        var zipEntry = archive.CreateEntry(fileName, CompressionLevel.Fastest);

                        // Write the downloaded file into the zip entry
                        using (var entryStream = zipEntry.Open())
                        {
                            await entryStream.WriteAsync(fileContent, 0, fileContent.Length);
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
}
