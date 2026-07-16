using Discord.WebSocket;
using Inkluzitron.Data;
using Inkluzitron.Utilities;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Handlers;

public class UserIsMemberHandler : IHandler
{
    private DiscordSocketClient DiscordSocketClient { get; }
    private DatabaseFactory DatabaseFactory { get; }

    public UserIsMemberHandler(DiscordSocketClient discordSocketClient, DatabaseFactory databaseFactory)
    {
        DiscordSocketClient = discordSocketClient;
        DatabaseFactory = databaseFactory;

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
            dbUser.IsMember = true;
            await ctx.SaveChangesAsync();
        });
    }

    private async Task OnGuildAvailableAsync(SocketGuild guild)
    {
        _ = Task.Run(async () =>
        {
            await guild.DownloadUsersAsync().ConfigureAwait(false);

            var currentMemberIds = guild.Users.Select(u => u.Id).ToHashSet();
            var lastId = 0ul;

            while (true)
            {
                using var ctx = DatabaseFactory.Create();

                var noMoreUsers = await Patiently.HandleDbConcurrency(async () =>
                {
                    var batch = await ctx.Users.OrderBy(user => user.Id).Where(u => u.Id > lastId).Take(20).ToListAsync();
                    foreach (var dbUser in batch)
                    {
                        dbUser.IsMember = currentMemberIds.Contains(dbUser.Id);
                    }

                    await ctx.SaveChangesAsync();

                    if (batch.Count == 0)
                        return true;

                    lastId = batch.Last().Id;
                    return false;
                });

                if (noMoreUsers)
                    break;
            }
        });
    }
}
