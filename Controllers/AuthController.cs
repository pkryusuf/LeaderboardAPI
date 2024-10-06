using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LeaderboardAPI.Data;
using LeaderboardAPI.Models;
using Microsoft.EntityFrameworkCore;
using LeaderboardAPI.Data.DTOs;
using LeaderboardAPI.Services;

namespace LeaderboardAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly LeaderboardContext _context;
        private readonly TokenService _tokenService;

        public AuthController(LeaderboardContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var player = new Player
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                DeviceId = dto.DeviceId,
                RegistrationDate = DateTime.Now
            };

            _context.Players.Add(player);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Player registered successfully!" });
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Username == dto.Username);
            if (player == null || !BCrypt.Net.BCrypt.Verify(dto.Password, player.PasswordHash))
                return Unauthorized(new { Message = "Invalid credentials" });

            var token = _tokenService.GenerateToken(player);
            return Ok(new { Token = token });
        }

    }
}
