using KingOfTheColonyApi.Models.Dto;
using KingOfTheColonyApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KingOfTheColonyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth = auth;

    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        if (!_auth.IsGoogleAuthEnabled())
            return NotFound(new { message = "La autenticacion con Google esta deshabilitada." });

        try
        {
            var response = await _auth.AuthenticateWithGoogleAsync(request.IdToken);
            return Ok(response);
        }
        catch (Google.Apis.Auth.InvalidJwtException)
        {
            return Unauthorized(new { message = "Token de Google inválido." });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] EmailRegisterRequest request)
    {
        try
        {
            var response = await _auth.RegisterWithEmailAsync(request.Email, request.Password, request.DisplayName);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] EmailLoginRequest request)
    {
        try
        {
            var response = await _auth.LoginWithEmailAsync(request.Email, request.Password);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
