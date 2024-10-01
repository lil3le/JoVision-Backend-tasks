using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BackEndTasks.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Task48Controller : ControllerBase
    {
        public class Upload
        {
            public IFormFile? File { get; set; }
            public string? Owner { get; set; }
        }

        // POST api/task48/update
        [HttpPost("Update")]
        public async Task<IActionResult> Update([FromForm] Upload uploadFile)
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

            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            var filePath = Path.Combine(folderPath, file.FileName);
            var metaDataFilePath = Path.Combine(folderPath, $"{Path.GetFileNameWithoutExtension(file.FileName)}.json");

            if (!System.IO.File.Exists(filePath) || !System.IO.File.Exists(metaDataFilePath))
                return BadRequest("File or metadata not found");

            try
            {
                var metaDataJson = System.IO.File.ReadAllText(metaDataFilePath);
                dynamic metaData = JsonConvert.DeserializeObject<dynamic>(metaDataJson);

                if (metaData.Owner != owner)
                    return Forbid("You are not authorized to update this file");

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                metaData.LastModificationTime = DateTime.Now;

                metaDataJson = JsonConvert.SerializeObject(metaData);
                await System.IO.File.WriteAllTextAsync(metaDataFilePath, metaDataJson);

                return Ok("File and metadata updated successfully");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the file");
            }
        }

        // GET api/task48/retrieve
        [HttpGet("Retrieve")]
        public IActionResult Retrieve([FromQuery] string fileName, [FromQuery] string fileOwner)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(fileOwner))
                return BadRequest("FileName and FileOwner must be provided");

            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            var filePath = Path.Combine(folderPath, fileName);
            var metaDataFilePath = Path.Combine(folderPath, $"{Path.GetFileNameWithoutExtension(fileName)}.json");

            if (!System.IO.File.Exists(filePath) || !System.IO.File.Exists(metaDataFilePath))
                return NotFound("File or metadata not found");

            try
            {
                var metaDataJson = System.IO.File.ReadAllText(metaDataFilePath);
                dynamic metaData = JsonConvert.DeserializeObject<dynamic>(metaDataJson);

                if (metaData.Owner != fileOwner)
                    return Forbid("You are not authorized to retrieve this file");

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, "application/octet-stream", fileName);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the file");
            }
        }
    }
}
