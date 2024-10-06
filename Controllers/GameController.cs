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
        private readonly ILogger<GameController> _logger;

        public GameController(LeaderboardContext context, RedisCacheService redisCache, ILogger<GameController> logger)
        {
            _context = context;
            _redisCache = redisCache;
            _logger = logger;
        }

        [HttpPost("submit-score")]
        [Authorize]
        public async Task<IActionResult> SubmitScore(SubmitScoreDto dto)
        {
            var playerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            _logger.LogInformation("Skor gönderme işlemi başlatıldı. Oyuncu ID: {PlayerId}, Skor: {MatchScore}", playerId, dto.MatchScore);

            var newScore = dto.MatchScore;

            int retryCount = 3;
            bool success = false;

            _logger.LogInformation("Transaction başlatıldı. Oyuncu ID: {PlayerId}", playerId);

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var score = new Score
                    {
                        PlayerId = playerId,
                        MatchScore = newScore,
                        ScoreTime = DateTime.UtcNow
                    };

                    _context.Scores.Add(score);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Veritabanına yeni skor kaydedildi. Oyuncu ID: {PlayerId}, Skor: {MatchScore}", playerId, newScore);

                    PlayerLeaderboardDto playerData = await UpdateRedisCacheWithRetries(playerId, newScore, retryCount);
                    if (playerData == null)
                    {
                        _logger.LogError("Redis güncellenemedi. Oyuncu ID: {PlayerId}", playerId);
                        throw new Exception("Redis güncellenemedi");
                    }
                    _logger.LogInformation("Skor Redis cache'e kaydedildi. Oyuncu ID: {PlayerId}, Toplam Skor: {TotalScore}", playerId, playerData.TotalScore);

                    await transaction.CommitAsync();
                    _logger.LogInformation("Transaction başarıyla tamamlandı. Oyuncu ID: {PlayerId}", playerId);
                    success = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Transaction geri alınıyor. Oyuncu ID: {PlayerId}", playerId);
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { Message = "Bir hata oluştu: " + ex.Message });
                }
            }

            return success ? Ok(new { Message = "Score submitted and saved successfully" }) : StatusCode(500, new { Message = "Score could not be saved" });
        }

        private async Task<PlayerLeaderboardDto> UpdateRedisCacheWithRetries(int playerId, int newScore, int retryCount)
        {
            PlayerLeaderboardDto playerData = null;

            for (int attempt = 0; attempt < retryCount; attempt++)
            {
                try
                {
                    var playerScoreJson = await _redisCache.GetStringAsync($"leaderboard:{playerId}");
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
                    _logger.LogInformation("Oyuncu bilgileri Redis'e başarıyla kaydedildi. Oyuncu ID: {PlayerId}, Toplam Skor: {TotalScore}", playerId, playerData.TotalScore);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Redis güncelleme başarısız. Deneme: {Attempt}, Oyuncu ID: {PlayerId}", attempt + 1, playerId);
                    if (attempt == retryCount - 1) return null;
                }
            }

            return playerData;
        }

        [HttpGet("leaderboard")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLeaderboard()
        {
            _logger.LogInformation("Liderlik tablosu sorgulandı.");

            var leaderboard = await _redisCache.GetLeaderboardAsync();

            _logger.LogInformation("Liderlik tablosu başarıyla alındı. Oyuncu sayısı: {Count}", leaderboard.Count);

            return Ok(leaderboard);
        }
    }

}
