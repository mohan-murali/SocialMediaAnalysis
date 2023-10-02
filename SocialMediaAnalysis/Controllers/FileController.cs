using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMediaAnalysis.Service;

namespace SocialMediaAnalysis.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FileController : Controller
{
    private readonly ICsvService _csvService;

    public FileController(ICsvService csvService)
    {
        _csvService = csvService;
    }

    [HttpPost]
    // [RequestFormLimits(MultipartBodyLengthLimit = 268435456)]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        try
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var email = claimsIdentity.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("could not get email from JWT");
            await _csvService.ReadCsvFile(file, email);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}