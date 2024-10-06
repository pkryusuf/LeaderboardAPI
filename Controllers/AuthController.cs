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
        private readonly ILogger<AuthController> _logger;


        public AuthController(LeaderboardContext context, TokenService tokenService, ILogger<AuthController> logger)
        {
            _context = context;
            _tokenService = tokenService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            _logger.LogInformation("Kay�t i�lemi ba�lat�ld�. Kullan�c� ad�: {Username}", dto.Username);

            var player = new Player
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                DeviceId = dto.DeviceId,
                RegistrationDate = DateTime.UtcNow
            };

            _context.Players.Add(player);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Kay�t i�lemi ba�ar�yla tamamland�. Kullan�c� ad�: {Username}", dto.Username);


            return Ok(new { Message = "Player registered successfully!" });
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            _logger.LogInformation("Giri� i�lemi ba�lat�ld�. Kullan�c� ad�: {Username}", dto.Username);

            var player = await _context.Players.FirstOrDefaultAsync(p => p.Username == dto.Username);
            if (player == null || !BCrypt.Net.BCrypt.Verify(dto.Password, player.PasswordHash))
            {
                _logger.LogWarning("Ge�ersiz giri� denemesi. Kullan�c� ad�: {Username}", dto.Username);
                return Unauthorized(new { Message = "Invalid credentials" });
            }

            var token = _tokenService.GenerateToken(player);
            _logger.LogInformation("Giri� ba�ar�l�. Kullan�c� ad�: {Username}", dto.Username);

            return Ok(new { Token = token });
        }

    }
}
