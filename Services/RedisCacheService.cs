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
            foreach (var player in leaderboard)
            {
                await _redisDb.StringSetAsync($"leaderboard:{player.PlayerId}", JsonConvert.SerializeObject(player));
                _logger.LogInformation("Liderlik tablosu Redis'e kaydedildi. Oyuncu ID: {PlayerId}, Toplam Skor: {TotalScore}", player.PlayerId, player.TotalScore);

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

                // Redis çöktüyse PostgreSQL'den liderlik tablosunu yükle
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

                // Redis'e geri yazalım
                await CacheLeaderboardAsync(playersFromDb);
                _logger.LogInformation("PostgreSQL'den alınan liderlik tablosu Redis'e kaydedildi. Oyuncu sayısı: {Count}", playersFromDb.Count);

                leaderboard = playersFromDb;
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
                        _logger.LogInformation("Oyuncu Redis'ten alındı. Oyuncu ID: {PlayerId}, Toplam Skor: {TotalScore}", playerData.PlayerId, playerData.TotalScore);
                    }
                }

                _logger.LogInformation("Liderlik tablosu Redis'ten başarıyla toplandı. Sıralama başlatılıyor...");
                
                leaderboard = leaderboard
                    .OrderByDescending(p => p.TotalScore)
                    .ThenBy(p => p.RegistrationDate)
                    .ThenByDescending(p => p.PlayerLevel)
                    .ThenByDescending(p => p.TrophyCount)
                    .ToList();

                _logger.LogInformation("Liderlik tablosu başarıyla sıralandı.");
            }

            return leaderboard;
        }

        public async Task<string> GetStringAsync(string key)
        {
            _logger.LogInformation("Redis'ten veri alınıyor. Key: {Key}", key);
            return await _redisDb.StringGetAsync(key);
        }

        public async Task SetStringAsync(string key, string value)
        {
            _logger.LogInformation("Redis'e veri kaydediliyor. Key: {Key}", key);
            await _redisDb.StringSetAsync(key, value);
        }


    }
}
