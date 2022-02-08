using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace BotExample
{
    internal class ModCommands : CommandGroup
    {

        private readonly FeedbackService _feedbackService;
        private readonly ICommandContext _context;
        private readonly IDiscordRestGuildAPI _restGuildApi;
        private readonly IDiscordRestChannelAPI _restChannelApi;
        private readonly ILogger<Program> _log;
        public ModCommands(FeedbackService feedbackService, ICommandContext context, IDiscordRestGuildAPI restGuildApi, IDiscordRestChannelAPI restChannelApi, ILogger<Program> log)
        {
            _feedbackService = feedbackService;
            _context = context;
            _restGuildApi = restGuildApi;
            _restChannelApi = restChannelApi;
            _log = log;
        }
        
        [Command("get-cuss-count")]
        [Description("Gets the number of times a user has cussed in this server")]
        public async Task<Result> CussCount([Description("The User to count, leave blank for self")] IUser? user = null,
            [Description("Channel to count in")][ChannelTypes(ChannelType.GuildText)] IChannel channel = null)
        {
            user ??= _context.User;
            await using Database.CussDbContext database = new();
            int count = database.CussLogs.Count(cl => cl.UserId == user.ID.Value);
            string contents = $"{Program.ToUserMention(user)} has cussed {count} time{(count == 1 ? "" : "s")}";
            Result<IReadOnlyList<IMessage>> reply = await _feedbackService.SendContextualSuccessAsync(
                contents);
            _log.LogInformation(contents);
            return !reply.IsSuccess
                ? Result.FromError(reply)
                : Result.FromSuccess();
        }

        [RequireDiscordPermission(DiscordPermission.BanMembers)]
        [Command("ban-who-said")]
        [Description("Ban anyone who said given word")]
        public async Task<Result> BanWhoSaid([Description("The word to ban people for")] string badWord, [Description("Timeout instead of ban")]bool shouldTimeout = false)
        {
            if (string.IsNullOrWhiteSpace(badWord))
            {
                Result<IReadOnlyList<IMessage>> errReply = await _feedbackService.SendContextualErrorAsync($"No bad word specified");
                return !errReply.IsSuccess
                    ? Result.FromError(errReply)
                    : Result.FromSuccess();
            }
            if (!BadWords.Instance.Contains(badWord.ToLower()))
            {
                Result<IReadOnlyList<IMessage>> errReply = await _feedbackService.SendContextualErrorAsync($"{badWord} is not a bad word");
                return !errReply.IsSuccess
                    ? Result.FromError(errReply)
                    : Result.FromSuccess();
            }

            Result<IReadOnlyList<IGuildMember>> serverMembers = await _restGuildApi.ListGuildMembersAsync(_context.GuildID.Value);
            Database.CussDbContext database = new();
            List<Database.CussLog> usersToBan = await database.CussLogs.Where(cl => cl.CussWord == badWord).ToListAsync();
            usersToBan = usersToBan.DistinctBy(cl => cl.UserId).ToList();
            // usersToBan = usersToBan.Where(cl => serverMembers.Entity.Any(sm => sm.User.Value.ID.Value == cl.UserId)).ToList();
            string msg = shouldTimeout ? "Timeouted:" : "Banned:";
            foreach (Database.CussLog cussLog in usersToBan)
            {
                if (!shouldTimeout){
                    Result banResult = await _restGuildApi.CreateGuildBanAsync(_context.GuildID.Value,
                        new Snowflake(cussLog.UserId),
                        reason: $"Cussin' {badWord}");
                    if (!banResult.IsSuccess)
                    {
                        _log.LogError(banResult.Error.ToString());
                        msg += $"\nCouldn't ban <@{cussLog.UserId}>\n{banResult.Error}";
                        continue;
                    }
                }
                else
                {
                    Result timeoutResult = await _restGuildApi.ModifyGuildMemberAsync(_context.GuildID.Value,
                        new Snowflake(cussLog.UserId),communicationDisabledUntil: DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1),
                        reason: $"Cussin' {badWord}");
                    if (!timeoutResult.IsSuccess)
                    {
                        _log.LogError(timeoutResult.Error.ToString());
                        msg += $"\nCouldn't timeout <@{cussLog.UserId}>\n{timeoutResult.Error}";
                        continue;
                    }
                }

                // _log.LogDebug($"Banned <@{cussLog.UserId}>");
                msg += $"\n<@{cussLog.UserId}>";
            }

            Result<IReadOnlyList<IMessage>> msgResult = await _feedbackService.SendContextualContentAsync(msg, Color.DarkRed);
            return !msgResult.IsSuccess
                ? Result.FromError(msgResult)
                : Result.FromSuccess();

        }

    }
}
