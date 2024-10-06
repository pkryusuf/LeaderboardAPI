using LeaderboardAPI.Data.DTOs;
using LeaderboardAPI.Models;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace LeaderboardAPI.Services
{
    public class RedisCacheService
    {
        private readonly IDatabase _redisDb;

        public RedisCacheService(IConnectionMultiplexer redis)
        {
            _redisDb = redis.GetDatabase();
        }

        public async Task CacheLeaderboardAsync(List<PlayerLeaderboardDto> leaderboard)
        {
            foreach (var player in leaderboard)
            {
                await _redisDb.StringSetAsync($"leaderboard:{player.PlayerId}", JsonConvert.SerializeObject(player));
            }
        }

        public async Task<List<PlayerLeaderboardDto>> GetLeaderboardAsync()
        {
            var redisKeys = (RedisValue[])_redisDb.Execute("KEYS", "leaderboard:*");
            var leaderboard = new List<PlayerLeaderboardDto>();

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




    }

}
