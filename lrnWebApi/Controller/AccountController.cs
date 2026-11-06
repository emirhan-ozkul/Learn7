using System.Security.Claims;
using lrnWebApi.Models;
using lrnWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace lrnWebApi.Controller;

[ApiController]
[Route("api/[controller]")]
public sealed class AccountController : ControllerBase
{
    private readonly UserAccountService _accounts;
    private readonly UserApiKeyStore _apiKeys;

    public AccountController(UserAccountService accounts, UserApiKeyStore apiKeys)
    {
        _accounts = accounts;
        _apiKeys = apiKeys;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        try
        {
            var session = _accounts.Register(request.Username, request.Email, request.Password);
            return Ok(new { token = session.Token, username = session.Username });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            return Conflict(new { error = "Bu kullanıcı adı veya e-posta zaten kayıtlı." });
        }
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { error = "E-posta ve parola zorunludur." });
        }

        var session = _accounts.Login(request.Email, request.Password);
        return session is null
            ? Unauthorized(new { error = "E-posta veya parola hatalı." })
            : Ok(new { token = session.Token, username = session.Username });
    }

    [Authorize]
    [HttpGet("api-key")]
    public IActionResult GetApiKeyStatus()
    {
        return Ok(new { configured = _apiKeys.HasApiKey(GetUserId()) });
    }

    [Authorize]
    [HttpPut("api-key")]
    public IActionResult SaveApiKey([FromBody] ApiKeyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey) || request.ApiKey.Length > 500)
        {
            return BadRequest(new { error = "Geçerli bir API anahtarı girin." });
        }

        _apiKeys.SaveApiKey(GetUserId(), request.ApiKey.Trim());
        return NoContent();
    }

    [Authorize]
    [HttpDelete("api-key")]
    public IActionResult DeleteApiKey()
    {
        _apiKeys.DeleteApiKey(GetUserId());
        return NoContent();
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        var authorization = Request.Headers.Authorization.ToString();
        _accounts.RevokeSession(authorization["Bearer ".Length..].Trim());
        return NoContent();
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
