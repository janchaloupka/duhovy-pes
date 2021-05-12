﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Inkluzitron.Contracts;
using Inkluzitron.Data;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.BdsmTestOrg
{
    public class QuizEmbedManager : IReactionHandler
    {
        protected BotDatabaseContext DbContext { get; }
        protected ReactionSettings ReactionSettings { get; }
        protected BdsmTestOrgSettings Settings { get; }
        protected DiscordSocketClient Client { get; }

        public QuizEmbedManager(BotDatabaseContext dbContext, ReactionSettings reactionSettings, BdsmTestOrgSettings settings,
            DiscordSocketClient client)
        {
            DbContext = dbContext;
            ReactionSettings = reactionSettings;
            Settings = settings;
            Client = client;
        }

        public async Task<bool> HandleReactionAddedAsync(IUserMessage message, IEmote reaction, IUser user, IUser botUser)
        {
            if (message.ReferencedMessage == null || message.Embeds.Count != 1)
                return false;

            var embed = message.Embeds.Single();

            if (!embed.TryParseMetadata<QuizEmbedMetadata>(out var metadata))
                return false;

            if (!ReactionSettings.PaginationReactionsWithRemoval.Contains(reaction))
                return false;

            var currentPageResultWasRemoved = false;
            if (metadata.UserId == user.Id && reaction.Equals(ReactionSettings.Remove))
            {
                var result = await DbContext.BdsmTestOrgQuizResults.FindAsync(metadata.ResultId);
                if (result != null)
                {
                    DbContext.BdsmTestOrgQuizResults.Remove(result);
                    await DbContext.SaveChangesAsync();
                    currentPageResultWasRemoved = true;
                }
            }

            if (!currentPageResultWasRemoved)
                currentPageResultWasRemoved = (await DbContext.BdsmTestOrgQuizResults.FindAsync(metadata.ResultId)) is null;

            var quizResultsOfUser = DbContext
                .BdsmTestOrgQuizResults
                .Include(x => x.Items)
                .AsQueryable()
                .Where(r => r.SubmittedById == metadata.UserId)
                .OrderByDescending(r => r.SubmittedAt);

            var count = await quizResultsOfUser.CountAsync();

            var formerPageNumber = metadata.PageNumber;
            int newPageNumber;
            if (reaction.Equals(ReactionSettings.MoveToFirst))
                newPageNumber = 1;
            else if (reaction.Equals(ReactionSettings.MoveToPrevious))
                newPageNumber = formerPageNumber - 1;
            else if (reaction.Equals(ReactionSettings.MoveToNext))
                newPageNumber = formerPageNumber + 1;
            else if (reaction.Equals(ReactionSettings.MoveToLast))
                newPageNumber = count;
            else
                newPageNumber = formerPageNumber;

            if (newPageNumber < 1)
                newPageNumber = 1;
            else if (newPageNumber > count)
                newPageNumber = count;

            if (newPageNumber != formerPageNumber || currentPageResultWasRemoved)
            {
                var newResultToDisplay = await quizResultsOfUser
                    .Skip(newPageNumber - 1)
                    .FirstOrDefaultAsync();

                var newEmbed = new EmbedBuilder().WithAuthor(user);

                if (newResultToDisplay is null)
                    newEmbed = newEmbed.WithBdsmTestOrgQuizInvitation(Settings, user);
                else
                    newEmbed = newEmbed.WithBdsmTestOrgQuizResult(Settings, newResultToDisplay, newPageNumber, count);

                await message.ModifyAsync(p => p.Embed = newEmbed.Build());
            }

            var context = new CommandContext(Client, message.ReferencedMessage);

            if (!context.IsPrivate)
                await message.RemoveReactionAsync(reaction, user);
            return true;
        }
    }
}