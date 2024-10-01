using Microsoft.AspNetCore.Mvc;

namespace Task44.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GreeterController : ControllerBase
    {
        [HttpGet("Greeter_Task44")]
        public IActionResult Get([FromQuery] string? name)
        {
            string greetname = string.IsNullOrEmpty(name) ? "anonymous" : name;
            return Ok($"hello {greetname}");
        }
    }
}
