﻿/// This file is part of Sanara.
///
/// Sanara is free software: you can redistribute it and/or modify
/// it under the terms of the GNU General Public License as published by
/// the Free Software Foundation, either version 3 of the License, or
/// (at your option) any later version.
///
/// Sanara is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
/// GNU General Public License for more details.
///
/// You should have received a copy of the GNU General Public License
/// along with Sanara.  If not, see<http://www.gnu.org/licenses/>.
using Discord;
using Discord.Commands;
using SanaraV2.Features.Entertainment;
using System;
using System.Threading.Tasks;

namespace SanaraV2.Modules.Entertainment
{
    public class AnimeManga : ModuleBase
    {
        Program p = Program.p;

        [Command("Subscribe Anime")]
        public async Task Subscribe(params string[] args)
        {
            Base.Utilities.CheckAvailability(Context.Guild.Id, Program.Module.AnimeManga);
            await p.DoAction(Context.User, Context.Guild.Id, Program.Module.AnimeManga);
            if (!Tools.Settings.CanModify(Context.User, Context.Guild.OwnerId))
            {
                await ReplyAsync(Base.Sentences.OnlyOwnerStr(Context.Guild.Id, Context.Guild.OwnerId));
            }
            else
            {
                var result = await Features.Entertainment.AnimeManga.Subscribe(Context.Guild, Program.p.db, args);
                switch (result.error)
                {
                    case Error.Subscribe.Help:
                        await ReplyAsync(Sentences.SubscribeHelp(Context.Guild.Id));
                        break;

                    case Error.Subscribe.InvalidChannel:
                        await ReplyAsync(Sentences.InvalidChannel(Context.Guild.Id));
                        break;

                    case Error.Subscribe.None:
                        await ReplyAsync(Sentences.SubscribeDone(Context.Guild.Id, "anime", result.answer.chan));
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        [Command("Unsubscribe Anime")]
        public async Task Unsubcribe(params string[] args)
        {
            Base.Utilities.CheckAvailability(Context.Guild.Id, Program.Module.AnimeManga);
            await p.DoAction(Context.User, Context.Guild.Id, Program.Module.AnimeManga);

            if (!Tools.Settings.CanModify(Context.User, Context.Guild.OwnerId))
            {
                await ReplyAsync(Base.Sentences.OnlyOwnerStr(Context.Guild.Id, Context.Guild.OwnerId));
            }
            else
            {
                var result = await Features.Entertainment.AnimeManga.Unsubscribe(Context.Guild, Program.p.db);
                switch (result.error)
                {
                    case Error.Unsubscribe.NoSubscription:
                        await ReplyAsync(Sentences.NoSubscription(Context.Guild.Id));
                        break;

                    case Error.Unsubscribe.None:
                        await ReplyAsync(Sentences.UnsubscribeDone(Context.Guild.Id, "anime"));
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        [Command("Anime", RunMode = RunMode.Async), Summary("Give informations about an anime using Kitsu API")]
        public async Task Anime(params string[] animeNameArr)
        {
            Base.Utilities.CheckAvailability(Context.Guild.Id, Program.Module.AnimeManga);
            await p.DoAction(Context.User, Context.Guild.Id, Program.Module.AnimeManga);
            var result = await Features.Entertainment.AnimeManga.SearchAnime(Features.Entertainment.AnimeManga.SearchType.Anime, animeNameArr, Program.p.kitsuAuth);
            switch (result.error)
            {
                case Error.AnimeManga.Help:
                    await ReplyAsync(Sentences.AnimeHelp(Context.Guild.Id));
                    break;

                case Error.AnimeManga.NotFound:
                    await ReplyAsync(Sentences.AnimeNotFound(Context.Guild.Id));
                    break;

                case Error.AnimeManga.None:
                    if (result.answer.nsfw && !((ITextChannel)Context.Channel).IsNsfw)
                        await ReplyAsync(Base.Sentences.AnswerNsfw(Context.Guild.Id));
                    else
                        await ReplyAsync("", false, CreateEmbed(true, result.answer, Context.Guild.Id));
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        [Command("Manga", RunMode = RunMode.Async), Summary("Give informations about a manga using Kitsu API")]
        public async Task Manga(params string[] mangaNameArr)
        {
            Base.Utilities.CheckAvailability(Context.Guild.Id, Program.Module.AnimeManga);
            await p.DoAction(Context.User, Context.Guild.Id, Program.Module.AnimeManga);
            var result = await Features.Entertainment.AnimeManga.SearchAnime(Features.Entertainment.AnimeManga.SearchType.Manga, mangaNameArr, Program.p.kitsuAuth);
            switch (result.error)
            {
                case Error.AnimeManga.Help:
                    await ReplyAsync(Sentences.MangaHelp(Context.Guild.Id));
                    break;

                case Error.AnimeManga.NotFound:
                    await ReplyAsync(Sentences.MangaNotFound(Context.Guild.Id));
                    break;

                case Error.AnimeManga.None:
                    if (result.answer.nsfw && !((ITextChannel)Context.Channel).IsNsfw)
                        await ReplyAsync(Base.Sentences.AnswerNsfw(Context.Guild.Id));
                    else
                        await ReplyAsync("", false, CreateEmbed(false, result.answer, Context.Guild.Id));
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        [Command("LN", RunMode = RunMode.Async), Alias("LightNovel", "Novel"), Summary("Give informations about a light novel using Kitsu API")]
        public async Task LN(params string[] mangaNameArr)
        {
            Base.Utilities.CheckAvailability(Context.Guild.Id, Program.Module.AnimeManga);
            await p.DoAction(Context.User, Context.Guild.Id, Program.Module.AnimeManga);
            var result = await Features.Entertainment.AnimeManga.SearchAnime(Features.Entertainment.AnimeManga.SearchType.LightNovel, mangaNameArr, Program.p.kitsuAuth);
            switch (result.error)
            {
                case Error.AnimeManga.Help:
                    await ReplyAsync(Sentences.LNHelp(Context.Guild.Id));
                    break;

                case Error.AnimeManga.NotFound:
                    await ReplyAsync(Sentences.LNNotFound(Context.Guild.Id));
                    break;

                case Error.AnimeManga.None:
                    if (result.answer.nsfw && !((ITextChannel)Context.Channel).IsNsfw)
                        await ReplyAsync(Base.Sentences.AnswerNsfw(Context.Guild.Id));
                    else
                        await ReplyAsync("", false, CreateEmbed(false, result.answer, Context.Guild.Id));
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private Embed CreateEmbed(bool isAnime, Response.AnimeManga res, ulong guildId)
        {
            string fullName = res.name + ((res.alternativeTitles == null || res.alternativeTitles.Length == 0) ? ("") : (" (" + string.Join(", ", res.alternativeTitles) + ")"));
            EmbedBuilder embed = new EmbedBuilder()
            {
                Title = fullName.Length > 256 ? res.name : fullName,
                Url = res.animeUrl,
                Color = Color.Green,
                ImageUrl = res.imageUrl,
                Description = res.synopsis
            };
            if (isAnime && res.episodeCount != null)
                embed.AddField(Sentences.AnimeEpisodes(Context.Guild.Id), res.episodeCount.Value + ((res.episodeLength != null) ? (" " + Sentences.AnimeLength(guildId, res.episodeLength.Value)) : ("")), true);
            if (res.rating != null)
                embed.AddField(Sentences.AnimeRating(Context.Guild.Id), res.rating.Value, true);
            embed.AddField(Sentences.ReleaseDate(Context.Guild.Id), ((res.startDate != null) ? res.startDate.Value.ToString(Base.Sentences.DateHourFormatShort(guildId)) + " - " + ((res.endDate != null) ? (res.endDate.Value.ToString(Base.Sentences.DateHourFormatShort(guildId))) : (Sentences.Unknown(guildId))) : (Sentences.ToBeAnnounced(guildId))), true);
            if (!string.IsNullOrEmpty(res.ageRating))
                embed.AddField(Sentences.AnimeAudiance(Context.Guild.Id), res.ageRating, true);
            return embed.Build();
        }
    }
}