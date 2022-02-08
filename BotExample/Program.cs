using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Commands.Extensions;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Hosting.Extensions;
using Remora.Discord.Rest.Extensions;
using Remora.Rest.Core;
using Remora.Results;

namespace BotExample
{
    public class Program
    {
        // Get Token from Configuration file
        internal static readonly Configuration Config = Configuration.ReadConfig();

        // private static readonly IDiscordRestGuildAPI _restGuildAPI;
        public static ILogger<Program> log;

        public static async Task Main(string[] args)
        {
            //Build the service
            IHost? host = Host.CreateDefaultBuilder()
                .AddDiscordService(_ => Config.Token)
                .ConfigureServices(
                    (_, services) =>
                    {
                        services
                            .AddDbContext<Database.CussDbContext>()
                            .AddDiscordRest(_ => Config.Token)
                            .AddResponder<CussResponder>()
                            .AddDiscordCommands(true)
                            .AddCommandTree()
                                .WithCommandGroup<ModCommands>();
                    })
                .ConfigureLogging(
                    c => c
                        .AddConsole()
                        .AddFilter("System.Net.Http.HttpClient.*.LogicalHandler", LogLevel.Warning)
                        .AddFilter("System.Net.Http.HttpClient.*.ClientHandler", LogLevel.Warning)
                        .SetMinimumLevel(LogLevel.Trace)
                )
                .UseConsoleLifetime()
                .Build();
            IServiceProvider? services = host.Services;
            log = services.GetRequiredService<ILogger<Program>>();

            Snowflake? debugServer = null;
#if DEBUG
            string? debugServerString = Config.TestServerId;
            if (debugServerString is not null)
            {
                if (!DiscordSnowflake.TryParse(debugServerString, out debugServer))
                {
                    log.LogWarning("Failed to parse debug server from environment");
                }
            }
            else
            {
                log.LogWarning("No debug server specified");
            }
#endif
            SlashService slashService = services.GetRequiredService<SlashService>();
            Result checkSlashSupport = slashService.SupportsSlashCommands();
            if (!checkSlashSupport.IsSuccess)
            {
                log.LogWarning
                (
                    "The registered commands of the bot don't support slash commands: {Reason}",
                    checkSlashSupport.Error?.Message
                );
            }
            else
            {
                Result updateSlash = await slashService.UpdateSlashCommandsAsync(debugServer).ConfigureAwait(false);
                if (!updateSlash.IsSuccess)
                {
                    log.LogWarning("Failed to update slash commands: {Reason}", updateSlash.Error?.Message);
                }
            }
            await host.RunAsync().ConfigureAwait(false);

            Console.WriteLine("Bye bye");
        }

        internal static async Task<string> CheckBoosting(Snowflake server, IDiscordRestGuildAPI _restGuildAPI)
        {
            Result<IReadOnlyList<IGuildMember>> membersResult = await _restGuildAPI.ListGuildMembersAsync(server).ConfigureAwait(false);
            string messageString = string.Empty;
            if (!membersResult.IsSuccess)
            {
                log.LogError($"ListGuildMembers failed with code {membersResult.Error}");
                messageString += $"ListGuildMembers failed with code {membersResult.Error}";
                IResult? err = membersResult.Inner;
                while (err != null)
                {
                    log.LogError($"ListGuildMembers inner failed with code {membersResult.Error}");
                    messageString += $"ListGuildMembers inner failed with code {membersResult.Error}";
                    err = err.Inner;
                }

                return messageString;
            }

            IReadOnlyList<IGuildMember> members = membersResult.Entity;
            messageString += "Boost Report:";
            foreach (IGuildMember member in members)
            {
                messageString += "\n";
                if (member.PremiumSince.HasValue && member.PremiumSince.Value.HasValue)
                {
                    string? message = $"{ToUserMention(member.User.Value)} is boosting";
                    log.LogInformation(message);
                    messageString += message;
                }
                else
                {
                    string? message = $"{ToUserMention(member.User.Value)} is not boosting";
                    log.LogInformation(message);
                    messageString += message;
                }

            }
            return messageString;
        }
        public static string ToUserMention(IUser user) => ToUserMention(user.ID);
        public static string ToUserMention(Snowflake userSnowflake) => $"<@{userSnowflake.Value}>";
    }

}