using Microsoft.AspNetCore.Mvc;

namespace Kromer.Controllers.Krist;

[Route("api/krist/[controller]")]
[ApiController]
public class AddressesController : ControllerBase
{
    public async Task<ActionResult<bool>> Address()
    {
        return true;
    }
}