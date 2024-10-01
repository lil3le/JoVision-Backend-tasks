using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Task47.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploaderController : ControllerBase
    {
        public class Upload
        {
            public IFormFile? File { get; set; }
            public string? Owner { get; set; }
        }

        // POST api/uploader/create
        [HttpPost("Create_Task47")]
        public async Task<IActionResult> Create([FromForm] Upload uploadFile)
        {
            IFormFile? file = uploadFile.File;
            string owner = uploadFile.Owner;

            if (file == null || file.Length == 0)
                return BadRequest("No file selected");

            if (string.IsNullOrEmpty(owner))
                return BadRequest("No owner assigned");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || (extension != ".jpg" && extension != ".jpeg"))
                return BadRequest("Invalid file type");

            var newFileName = Path.GetFileNameWithoutExtension(file.FileName) + extension;

            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var filePath = Path.Combine(folderPath, newFileName);

            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    return Conflict("A file with the same name already exists.");
                }

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var metaData = new
                {
                    Owner = owner,
                    CreationTime = DateTime.Now,
                    LastModificationTime = DateTime.Now
                };

                var metaDataJson = JsonConvert.SerializeObject(metaData);
                var metaDataFilePath = Path.Combine(folderPath, $"{Path.GetFileNameWithoutExtension(newFileName)}.json");
                await System.IO.File.WriteAllTextAsync(metaDataFilePath, metaDataJson);

                var fileUrl = Url.Content($"~/uploads/{newFileName}");
                return Created(fileUrl, new { url = fileUrl });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while uploading the file");
            }
        }

        // DELETE api/uploader/delete
        [HttpGet("Delete_Task47")]
        public IActionResult Delete([FromQuery] string fileName, [FromQuery] string fileOwner)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(fileOwner))
                return BadRequest("FileName and FileOwner are required");

            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            var filePath = Path.Combine(folderPath, fileName);
            var metaDataFilePath = Path.Combine(folderPath, $"{Path.GetFileNameWithoutExtension(fileName)}.json");

            if (!System.IO.File.Exists(filePath) || !System.IO.File.Exists(metaDataFilePath))
                return BadRequest("File or metadata not found");

            try
            {
                var metaDataJson = System.IO.File.ReadAllText(metaDataFilePath);
                var metaData = JsonConvert.DeserializeObject<dynamic>(metaDataJson);

                if (metaData.Owner != fileOwner)
                    return Forbid("You are not authorized to delete this file");

                System.IO.File.Delete(filePath);
                System.IO.File.Delete(metaDataFilePath);

                return Ok("File and metadata deleted successfully");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the file");
            }
        }
    }
}
