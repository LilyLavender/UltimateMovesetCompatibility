using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace CustomCharInfo.server.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;

        public AccountController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration config
        )
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var user = new ApplicationUser { UserName = dto.Email, Email = dto.Email, UserTypeId = 1 };
            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);
            return Ok();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
                return Unauthorized();

            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.AddDays(7), signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized("User not found");

            var modder = await _context.Modders
                .Where(m => m.UserId == user.Id)
                .Select(m => new { m.ModderId })
                .FirstOrDefaultAsync();

            object result = new
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                UserTypeId = user.UserTypeId,
                ModderId = modder?.ModderId
            };

            if (modder == null)
            {
                var modderIdFuture = await _context.ActionLogs
                    .Where(a => a.UserId == user.Id && a.ItemTypeId == 2)
                    .OrderBy(a => a.CreatedAt)
                    .Select(a => (int?)a.ItemId)
                    .FirstOrDefaultAsync();

                result = new
                {
                    Id = user.Id,
                    Email = user.Email,
                    UserName = user.UserName,
                    UserTypeId = user.UserTypeId,
                    ModderId = (int?)null,
                    ModderIdFuture = modderIdFuture
                };
            }

            return Ok(result);
        }
        
        [Authorize]
        [HttpPut("edit-username")]
        public async Task<IActionResult> EditUsername(EditUsernameDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NewUsername))
                return BadRequest("Username cannot be empty");
        
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized("User not found");
        
            user.UserName = dto.NewUsername;
        
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);
        
            return Ok(new { message = "Username updated successfully", newUsername = user.UserName });
        }

    }
}
