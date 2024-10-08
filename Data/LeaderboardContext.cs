﻿using Microsoft.EntityFrameworkCore;
using LeaderboardAPI.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace LeaderboardAPI.Data
{
    public class LeaderboardContext : DbContext
    {
        public DbSet<Player> Players { get; set; }
        public DbSet<Score> Scores { get; set; }

        public LeaderboardContext(DbContextOptions<LeaderboardContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Player>().HasIndex(p => p.Username).IsUnique();
            modelBuilder.Entity<Score>().HasIndex(s => s.PlayerId);
            modelBuilder.Entity<Score>().HasIndex(s => s.MatchScore);
            modelBuilder.Entity<Player>().HasIndex(p => p.RegistrationDate);
            modelBuilder.Entity<Player>().HasIndex(p => p.PlayerLevel);
            modelBuilder.Entity<Player>().HasIndex(p => p.TrophyCount);
        }
    }

}
