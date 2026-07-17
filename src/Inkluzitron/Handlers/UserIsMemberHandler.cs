using Discord.WebSocket;
using Inkluzitron.Data;
using Inkluzitron.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Handlers;

public class UserIsMemberHandler : IHandler
{
    private DiscordSocketClient DiscordSocketClient { get; }
    private DatabaseFactory DatabaseFactory { get; }
    public ILogger<UserIsMemberHandler> Logger { get; }

    public UserIsMemberHandler(DiscordSocketClient discordSocketClient, DatabaseFactory databaseFactory, ILogger<UserIsMemberHandler> logger)
    {
        DiscordSocketClient = discordSocketClient;
        DatabaseFactory = databaseFactory;
        Logger = logger;
        DiscordSocketClient.UserJoined += OnUserJoinedAsync;
        DiscordSocketClient.UserLeft += OnUserLeftAsync;
        DiscordSocketClient.GuildAvailable += OnGuildAvailableAsync;
    }

    private async Task OnUserLeftAsync(SocketGuild guild, SocketUser user)
    {
        using var ctx = DatabaseFactory.Create();
        await Patiently.HandleDbConcurrency(async () =>
        {
            var dbUser = await ctx.Users.FindAsync(user.Id);
            if (dbUser == null)
              return;
            dbUser.IsMember = false;
            await ctx.SaveChangesAsync();
        });
    }

    private async Task OnUserJoinedAsync(SocketGuildUser user)
    {
        using var ctx = DatabaseFactory.Create();
        await Patiently.HandleDbConcurrency(async () =>
        {
            var dbUser = await ctx.Users.FindAsync(user.Id);
            if (dbUser == null)
              return;
            dbUser.IsMember = true;
            await ctx.SaveChangesAsync();
        });
    }

    private async Task OnGuildAvailableAsync(SocketGuild guild)
    {
        _ = Task.Run(async () =>
        {
            Logger.LogInformation("OnGuildAvailableAsync: Downloading all users.");
            await guild.DownloadUsersAsync().ConfigureAwait(false);

            Logger.LogInformation("OnGuildAvailableAsync: Downloaded all users, updating IsMember properties.");
            var currentMemberIds = guild.Users.Select(u => u.Id).ToHashSet();

            using var ctx = DatabaseFactory.Create();

            await Patiently.HandleDbConcurrency(async () =>
            {
                var batch = await ctx.Users.ToListAsync();
                foreach (var dbUser in batch)
                {
                    dbUser.IsMember = currentMemberIds.Contains(dbUser.Id);
                }

                Logger.LogInformation($"OnGuildAvailableAsync: Updated {batch.Count} users and their IsMember status");
                await ctx.SaveChangesAsync();
            });

            Logger.LogInformation("OnGuildAvailableAsync: Done updating IsMember properties.");
        });
    }
}
