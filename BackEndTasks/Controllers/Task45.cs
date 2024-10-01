using Microsoft.AspNetCore.Mvc;

namespace Task45.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BirthDateController : ControllerBase
    {
        [HttpGet("BirthDate_Task45")]
        public IActionResult Get([FromQuery] string? name, [FromQuery] int? years, [FromQuery] int? months, [FromQuery] int? days)
        {
            string greetName = string.IsNullOrEmpty(name) ? "anonymous" : name;

            if (years.HasValue && months.HasValue && days.HasValue)
            {
                try
                {
                    DateTime birthDate = new DateTime(years.Value, months.Value, days.Value);
                    int age = CalculateAge(birthDate);
                    string message = $"Hello {greetName}, your age is {age}";
                    return Ok(message);
                }
                catch (ArgumentOutOfRangeException)
                {
                    return BadRequest($"Hello {greetName}, the provided date is invalid.");
                }
            }
            else
            {
                return BadRequest($"Hello {greetName}, I can’t calculate your age without knowing your full birthdate.");
            }
        }

        private int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            int age = today.Year - birthDate.Year;

            if (birthDate.Date > today.AddYears(-age)) age--;

            return age;
        }
    }

    public class GreeterController : ControllerBase
    {
        [HttpGet("Greeter_Task45")]
        public IActionResult Get([FromQuery] string? name)
        {
            string greetname = string.IsNullOrEmpty(name) ? "anonymous" : name;
            return Ok($"hello {greetname}");
        }
    }

}
