using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace BotExample
{
    internal class Database
    {
        public class CussLog
        {
            public CussLog()
            {
            }
            public int LogId { get; set; }
            public ulong ServerId { get; set; }
            public ulong UserId { get; set; }
            public ulong ChannelId { get; set; }
            public string CussWord { get; set; } = null!;
        }

        public class CussLogEntityTypeConfiguration : IEntityTypeConfiguration<CussLog>
        {
            public void Configure(EntityTypeBuilder<CussLog> builder)
            {
                //Property Specific stuff
                builder.Property(cl => cl.ServerId).IsRequired();
                builder.Property(cl => cl.UserId).IsRequired();
                builder.Property(cl => cl.ChannelId).IsRequired();
                builder.Property(cl => cl.CussWord).IsRequired();
                //Table Stuff
                builder.ToTable("CussLog");
                builder.HasKey(cl => cl.LogId);
            }
        }

        public class CussDbContext : DbContext
        {
            public DbSet<CussLog> CussLogs { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
                optionsBuilder.UseSqlite("Data Source=CussDatabase.db;")
                    .LogTo((logtext) => Program.log.LogDebug(logtext))
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors();

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.ApplyConfigurationsFromAssembly(typeof(CussLogEntityTypeConfiguration).Assembly);
            }
        }

        public static async Task<bool> LogCuss(IUser user, IChannel channel, IGuild server, string cussWord) =>
           await LogCuss(user.ID, channel.ID, server.ID, cussWord);
        public static async Task<bool> LogCuss(Snowflake userId, Snowflake channelId, Snowflake serverId, string cussWord)
        {
            await using CussDbContext database = new();
            CussLog cussLog = new() { ChannelId = channelId.Value, ServerId = serverId.Value, UserId = userId.Value, CussWord = cussWord };
            await database.CussLogs.AddAsync(cussLog);
            return await database.SaveChangesAsync() > 0;
            
        }
    }
}
