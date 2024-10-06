using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using LeaderboardAPI.Data;
using LeaderboardAPI.Models;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LeaderboardAPI.Services;
using LeaderboardAPI.Data.DTOs;
using Newtonsoft.Json;

namespace LeaderboardAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly LeaderboardContext _context;
        private readonly RedisCacheService _redisCache;

        public GameController(LeaderboardContext context, RedisCacheService redisCache)
        {
            _context = context;
            _redisCache = redisCache;
        }

        [HttpPost("submit-score")]
        [Authorize]
        public async Task<IActionResult> SubmitScore(SubmitScoreDto dto)
        {
            var playerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var newScore = dto.MatchScore;

            var score = new Score
            {
                PlayerId = playerId,
                MatchScore = newScore,
                ScoreTime = DateTime.UtcNow
            };

            _context.Scores.Add(score);
            await _context.SaveChangesAsync();

            var playerScoreJson = await _redisCache.GetStringAsync($"leaderboard:{playerId}");
            PlayerLeaderboardDto playerData;

            var player = await _context.Players.FindAsync(playerId);

            if (!string.IsNullOrEmpty(playerScoreJson))
            {
                playerData = JsonConvert.DeserializeObject<PlayerLeaderboardDto>(playerScoreJson);
                playerData.TotalScore += newScore;  
            }
            else
            {
                playerData = new PlayerLeaderboardDto
                {
                    PlayerId = playerId,
                    TotalScore = newScore,
                    RegistrationDate = player.RegistrationDate,
                    PlayerLevel = player.PlayerLevel,
                    TrophyCount = player.TrophyCount
                };
            }

            await _redisCache.SetStringAsync($"leaderboard:{playerId}", JsonConvert.SerializeObject(playerData));

            return Ok(new { Message = "Score submitted and saved successfully" });
        }




        [HttpGet("leaderboard")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLeaderboard()
        {
            var leaderboard = await _redisCache.GetLeaderboardAsync();

            return Ok(leaderboard);
        }




    }
}
