using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HelloController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { Message = "Hello, World!", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("{name}")]
    public IActionResult Get(string name)
    {
        return Ok(new { Message = $"Hello, {name}!", Timestamp = DateTime.UtcNow });
    }
}