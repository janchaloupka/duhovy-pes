using Discord.WebSocket;
using Inkluzitron.Data;
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
        await ctx.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(u => u.SetProperty(dbUser => dbUser.IsMember, false));
    }

    private async Task OnUserJoinedAsync(SocketGuildUser user)
    {
        using var ctx = DatabaseFactory.Create();
        await ctx.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(u => u.SetProperty(dbUser => dbUser.IsMember, true));
    }

    private async Task OnGuildAvailableAsync(SocketGuild guild)
    {
        _ = Task.Run(async () =>
        {
            await guild.DownloadUsersAsync().ConfigureAwait(false);
            var currentMemberIds = guild.Users.Select(u => u.Id).ToList();

            using var ctx = DatabaseFactory.Create();
            await ctx.Users.Where(u => currentMemberIds.Contains(u.Id)).ExecuteUpdateAsync(u => u.SetProperty(dbUser => dbUser.IsMember, true));
            await ctx.Users.Where(u => !currentMemberIds.Contains(u.Id)).ExecuteUpdateAsync(u => u.SetProperty(dbUser => dbUser.IsMember, false));
        });
    }
}
