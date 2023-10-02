using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMediaAnalysis.Models;
using SocialMediaAnalysis.Service;

namespace SocialMediaAnalysis.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoginController : Controller
{
    private readonly ILoginService _loginService;

    public LoginController(ILoginService loginService)
    {
        _loginService = loginService;
    }

    [HttpPost]
    [Route("Register")]
    public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest request)
    {
        try
        {
            var user = new User(request.Email, request.Name, request.Password);
            var (name, token) = await _loginService.RegisterUser(user);
            return new RegisterResponse("Success", "", token, name);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        try
        {
            var (name, token) = await _loginService.LoginUser(request);
            return new LoginResponse("Success", "", token, name);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    //Test endpoint for login test
    [HttpGet, Authorize]
    public async Task<ActionResult<LoginResponse>> Test()
    {
        // await _csvService.ReadCsvFile(null);
        return new LoginResponse("Success", "", "", "");
    }
}