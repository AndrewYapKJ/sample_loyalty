using Microsoft.AspNetCore.Mvc;

namespace gussmann_loyalty_program.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok(new { status = "ok" });
    }
}
