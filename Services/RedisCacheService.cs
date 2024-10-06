using LeaderboardAPI.Data;
using LeaderboardAPI.Data.DTOs;
using LeaderboardAPI.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace LeaderboardAPI.Services
{
    public class RedisCacheService
    {
        private readonly IDatabase _redisDb;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly LeaderboardContext _context;


        public RedisCacheService(LeaderboardContext context, IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
        {
            _redisDb = redis.GetDatabase();
            _logger = logger;
            _context = context;
        }


        public async Task CacheLeaderboardAsync(List<PlayerLeaderboardDto> leaderboard)
        {
            var topPlayers = leaderboard
                .OrderByDescending(p => p.TotalScore)
                .ThenBy(p => p.RegistrationDate)
                .ThenByDescending(p => p.PlayerLevel)
                .ThenByDescending(p => p.TrophyCount)
                .Take(100)
                .ToList();

            foreach (var player in topPlayers)
            {
                await _redisDb.StringSetAsync($"leaderboard:{player.PlayerId}", JsonConvert.SerializeObject(player));
                _logger.LogInformation("Liderlik tablosunun ilk 100 oyuncusu Redis'e kaydedildi. Oyuncu ID: {PlayerId}, Toplam Skor: {TotalScore}", player.PlayerId, player.TotalScore);
            }
        }
        public async Task<List<PlayerLeaderboardDto>> GetLeaderboardAsync()
        {
            _logger.LogInformation("Liderlik tablosu Redis'ten alınmaya çalışılıyor.");

            var redisKeys = (RedisValue[])_redisDb.Execute("KEYS", "leaderboard:*");
            var leaderboard = new List<PlayerLeaderboardDto>();

            if (redisKeys.Length == 0)
            {
                _logger.LogWarning("Redis'te liderlik tablosu bulunamadı. PostgreSQL'den yükleniyor...");
                var playersFromDb = await _context.Players
                    .Include(p => p.Scores)
                    .Select(p => new PlayerLeaderboardDto
                    {
                        PlayerId = p.PlayerId,
                        TotalScore = p.Scores.Sum(s => s.MatchScore),
                        RegistrationDate = p.RegistrationDate,
                        PlayerLevel = p.PlayerLevel,
                        TrophyCount = p.TrophyCount
                    })
                    .ToListAsync();

                await CacheLeaderboardAsync(playersFromDb);
                _logger.LogInformation("PostgreSQL'den alınan liderlik tablosu Redis'e kaydedildi.");
                leaderboard = playersFromDb.Take(100).ToList();
            }
            else
            {
                _logger.LogInformation("Redis'ten liderlik tablosu alındı, oyuncu sayısı: {Count}", redisKeys.Length);

                foreach (var key in redisKeys)
                {
                    var redisKey = key.ToString();
                    var playerScoreJson = await _redisDb.StringGetAsync(redisKey);

                    if (!string.IsNullOrEmpty(playerScoreJson))
                    {
                        var playerData = JsonConvert.DeserializeObject<PlayerLeaderboardDto>(playerScoreJson);
                        leaderboard.Add(playerData);
                    }
                }

                leaderboard = leaderboard
                    .OrderByDescending(p => p.TotalScore)
                    .ThenBy(p => p.RegistrationDate)
                    .ThenByDescending(p => p.PlayerLevel)
                    .ThenByDescending(p => p.TrophyCount)
                    .ToList();
            }

            return leaderboard;
        }


        public async Task<string> GetStringAsync(string key)
        {
            return await _redisDb.StringGetAsync(key);
        }

        public async Task SetStringAsync(string key, string value)
        {
            await _redisDb.StringSetAsync(key, value);
        }
        public async Task DeleteFromLeaderboardAsync(int playerId)
        {
            await _redisDb.KeyDeleteAsync($"leaderboard:{playerId}");
            _logger.LogInformation("Oyuncu Redis'ten silindi. Oyuncu ID: {PlayerId}", playerId);
        }
        public async Task<RedisValue[]> GetLeaderboardKeysAsync()
        {
            var redisKeys = (RedisValue[])await _redisDb.ExecuteAsync("KEYS", "leaderboard:*");
            return redisKeys;
        }



    }
}
