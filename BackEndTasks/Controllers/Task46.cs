using Microsoft.AspNetCore.Mvc;

namespace Task46.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BirthDateController : ControllerBase
    {
        [HttpPost("BirthDate_Task46")]
        public IActionResult Get([FromBody] Person person)
        {
            string greetName = string.IsNullOrEmpty(person.name) ? "anonymous" : person.name;

            if (person.years != null && person.months != null && person.days != null)
            {
                try
                {
                    DateTime birthDate = new DateTime(person.years, person.months, person.days);
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

    public class Person
    {
        public string name { get; set; }
        public int years { get; set; }
        public int months { get; set; }
        public int days { get; set; }
    }

    public class GreeterController : ControllerBase
    {
        [HttpPost("Greeter_Task46")]
        public IActionResult Get([FromBody] Person person)
        {
            string greetname = string.IsNullOrEmpty(person.name) ? "anonymous" : person.name;
            return Ok($"hello {greetname}");
        }
    }

}
