using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BackEndTasks.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Task49Controller : ControllerBase
    {
        public class Upload
        {
            public IFormFile? File { get; set; }
            public string? Owner { get; set; }
        }

        public class FilterRequest
        {
            public DateTime? CreationDate { get; set; }
            public DateTime? ModificationDate { get; set; }
            public string? Owner { get; set; }
            public FilterType? FilterType { get; set; }
        }

        public enum FilterType
        {
            ByModificationDate,
            ByCreationDateDescending,
            ByCreationDateAscending,
            ByOwner
        }

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

        [HttpPost("Filter")]
        public IActionResult Filter([FromForm] FilterRequest filterRequest)
        {
            if (filterRequest.FilterType == null)
                return BadRequest("FilterType must be provided");

            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(folderPath))
                return NotFound("Upload directory not found");

            var metaDataFiles = Directory.GetFiles(folderPath, "*.json");
            var resultList = new List<dynamic>();

            try
            {
                foreach (var metaDataFile in metaDataFiles)
                {
                    var metaDataJson = System.IO.File.ReadAllText(metaDataFile);
                    dynamic metaData = JsonConvert.DeserializeObject<dynamic>(metaDataJson);

                    switch (filterRequest.FilterType)
                    {
                        case FilterType.ByModificationDate:
                            if (filterRequest.ModificationDate != null && metaData.LastModificationTime < filterRequest.ModificationDate)
                                resultList.Add(new { FileName = Path.GetFileNameWithoutExtension(metaDataFile), metaData.Owner });
                            break;

                        case FilterType.ByCreationDateDescending:
                        case FilterType.ByCreationDateAscending:
                            if (filterRequest.CreationDate != null && metaData.CreationTime > filterRequest.CreationDate)
                                resultList.Add(new { FileName = Path.GetFileNameWithoutExtension(metaDataFile), metaData.Owner });
                            break;

                        case FilterType.ByOwner:
                            if (!string.IsNullOrEmpty(filterRequest.Owner) && metaData.Owner == filterRequest.Owner)
                                resultList.Add(new { FileName = Path.GetFileNameWithoutExtension(metaDataFile), metaData.Owner });
                            break;

                        default:
                            return BadRequest("Invalid FilterType");
                    }
                }

                if (filterRequest.FilterType == FilterType.ByCreationDateAscending)
                    resultList = resultList.OrderBy(x => x.CreationTime).ToList();

                if (filterRequest.FilterType == FilterType.ByCreationDateDescending)
                    resultList = resultList.OrderByDescending(x => x.CreationTime).ToList();

                return Ok(resultList);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while filtering the files");
            }
        }

        [HttpGet("TransferOwnership")]
        public IActionResult TransferOwnership([FromQuery] string oldOwner, [FromQuery] string newOwner)
        {
            if (string.IsNullOrEmpty(oldOwner) || string.IsNullOrEmpty(newOwner))
                return BadRequest("OldOwner and NewOwner must be provided");

            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(folderPath))
                return NotFound("Upload directory not found");

            var metaDataFiles = Directory.GetFiles(folderPath, "*.json");
            var resultList = new List<dynamic>();

            try
            {
                foreach (var metaDataFile in metaDataFiles)
                {
                    var metaDataJson = System.IO.File.ReadAllText(metaDataFile);
                    dynamic metaData = JsonConvert.DeserializeObject<dynamic>(metaDataJson);

                    if (metaData.Owner == oldOwner)
                    {
                        metaData.Owner = newOwner;
                        metaDataJson = JsonConvert.SerializeObject(metaData);
                        System.IO.File.WriteAllText(metaDataFile, metaDataJson);
                    }

                    if (metaData.Owner == newOwner)
                    {
                        resultList.Add(new { FileName = Path.GetFileNameWithoutExtension(metaDataFile), metaData.Owner });
                    }
                }

                return Ok(resultList);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while transferring ownership");
            }
        }
    }
}
