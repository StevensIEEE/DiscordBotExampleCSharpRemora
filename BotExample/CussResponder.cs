using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace BotExample
{
    // All events are in Remora.Discord.API.Abstractions.Gateway.Events
    internal class CussResponder : IResponder<IMessageCreate>
    {

        private readonly IDiscordRestChannelAPI _channelAPI;
        private readonly IDiscordRestGuildAPI _guildAPI;
        private static readonly BadWords badWordList = BadWords.Instance;
        private readonly ILogger<Program> _log;
        public CussResponder(IDiscordRestChannelAPI channelApi, IDiscordRestGuildAPI guildApi, ILogger<Program> log)
        {
            _channelAPI = channelApi;
            _guildAPI = guildApi;
            _log = log;
        }
        public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = default)
        {
            if(!gatewayEvent.GuildID.HasValue) return Result.FromSuccess();
            string lowerMsgContent = gatewayEvent.Content.ToLower();
            foreach (string badWord in BadWords.Instance)
            {
                if (!lowerMsgContent.Contains(badWord)) continue;
                Result handleResult = await _channelAPI.DeleteMessageAsync(gatewayEvent.ChannelID, gatewayEvent.ID,
                    $"Used the bad word {badWord}", ct);
                if (!handleResult.IsSuccess)
                {
                    _log.LogCritical($"Could not remove message {gatewayEvent.ID}");
                    return handleResult;
                }

                Result<IMessage> replyResult = await _channelAPI.CreateMessageAsync(gatewayEvent.ChannelID,
                    $"{Program.ToUserMention(gatewayEvent.Author)}, no cussin'", ct: ct);
                if (!replyResult.IsSuccess)
                {
                    return Result.FromError(replyResult);
                }

                Task<Task<Result>> deleteResultTask = Task.Delay(3000, ct).ContinueWith((_) =>
                    _channelAPI.DeleteMessageAsync(replyResult.Entity.ChannelID, replyResult.Entity.ID, ct: ct), ct);
                await Database.LogCuss(gatewayEvent.Author.ID, gatewayEvent.ChannelID, gatewayEvent.GuildID.Value, badWord);
                return await await deleteResultTask;
            }
            return Result.FromSuccess();
        }
    }

    internal class BadWords : IEnumerable<string>
    {
        private readonly HttpClient _httpClient = new();
        private const string BadWordsSource =
            "https://raw.githubusercontent.com/turalus/encycloDB/master/Dirty%20Words/DirtyWords.json";

        private readonly JsonStructure _data;
        private readonly List<string> _badWordsEng;
        internal class Record
        {
            public string word { get; set; } = null!;
            public string language { get; set; } = null!;
        }

        internal class JsonStructure
        {
            public Record[] Records { get; set; } = null!;
        }

        private BadWords()
        {
            string jsonString = _httpClient.GetStringAsync(BadWordsSource).Result;
            JsonSerializerOptions? options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                WriteIndented = true
            };
            _data = JsonSerializer.Deserialize<JsonStructure>(jsonString, options) ?? throw new InvalidOperationException();
            _badWordsEng = _data.Records.Where(record => record.language == "en").Select(record => record.word)
                .ToList();
        }

        public static BadWords Instance { get; } = new();
        public IEnumerator<string> GetEnumerator()
        {
            return _badWordsEng.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_badWordsEng).GetEnumerator();
        }
    }
}
