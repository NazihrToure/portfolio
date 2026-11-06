using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Data;
using Portfolio.Api.DTOs;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly PortfolioDbContext _dbContext;

    public AuthController(UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager, PortfolioDbContext dbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
    }

    [HttpGet("csrf")]
    public IActionResult GetCsrfToken(IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(CredentialsDto dto)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        var user = new IdentityUser { UserName = dto.Email, Email = dto.Email };
        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(error => error.Description) });
        }

        result = await _userManager.AddToRoleAsync(user, "Gast");
        if (!result.Succeeded)
        {
            return Problem("De standaardrol kon niet worden toegekend.");
        }

        await transaction.CommitAsync();
        return StatusCode(StatusCodes.Status201Created, new { user.Email, roles = new[] { "Gast" } });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(CredentialsDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await _signInManager.PasswordSignInAsync(user, dto.Password,
            isPersistent: false, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return Unauthorized();
        }

        return Ok(new { user.Email, roles = await _userManager.GetRolesAsync(user) });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new { user.Email, roles = await _userManager.GetRolesAsync(user) });
    }
}
