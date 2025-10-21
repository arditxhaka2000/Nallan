using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.JwtService;
using System;
using System.Threading.Tasks;

[ApiController]
[Route("api/gjirafa")]
[AllowAnonymous]
public class GjirafaAuthController : ControllerBase
{
    private readonly IGjirafaPartnerService _partnerService;
    private readonly JwtService _jwtService;

    public GjirafaAuthController(IGjirafaPartnerService partnerService, JwtService jwtService)
    {
        _partnerService = partnerService;
        _jwtService = jwtService;
    }

    /// <summary>
    /// POST /api/gjirafa/login
    /// Gjirafa logs in with email/password, receives JWT token
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] GjirafaLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "Email and password are required" });
        }

        // Validate partner credentials
        var isValid = await _partnerService.ValidatePartnerAsync(request.Email, request.Password);

        if (!isValid)
        {
            return Unauthorized(new { error = "Invalid email or password" });
        }

        try
        {
            // Generate JWT token using your existing JwtService
            var token = _jwtService.GenerateSecurityToken(request.Email);

            return Ok(new GjirafaLoginResponse
            {
                Token = token,
                TokenType = "Bearer",
                Message = "Login successful"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Token generation failed", details = ex.Message });
        }
    }
}