/// This file is part of Sanara.
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
using Discord.Commands;
using SanaraV2.Modules.Base;
using System;
using System.Threading.Tasks;

namespace SanaraV2.Modules.GamesInfo
{
    [Group("Fire Emblem"), Alias("Fe")]
    public class FireEmblem : ModuleBase
    {
        [Command("", RunMode = RunMode.Async), Priority(-1)]
        public async Task CharacDefault(params string[] shipNameArr) => await Charac(shipNameArr);

        [Command("Charac", RunMode = RunMode.Async), Summary("Get informations about a character")]
        public async Task Charac(params string[] shipNameArr)
        {
            Utilities.CheckAvailability(Context.Guild.Id, Program.Module.Kancolle);
            await Program.p.DoAction(Context.User, Context.Guild.Id, Program.Module.Kancolle);
            var result = await Features.GamesInfo.Kancolle.SearchCharac(shipNameArr);
            switch (result.error)
            {
                case Features.GamesInfo.Error.Charac.Help:
                    await ReplyAsync("Help");
                    break;

                case Features.GamesInfo.Error.Charac.NotFound:
                    await ReplyAsync("NotFound");
                    break;

                case Features.GamesInfo.Error.Charac.None:
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
