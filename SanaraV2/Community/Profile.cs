﻿using Discord;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver;
using RethinkDb.Driver.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SanaraV2.Community
{
    public class Profile
    {
        /// <summary>
        /// Create empty profile
        /// </summary>
        public Profile(IUserMessage msg, IUser user)
        {
            _id = user.Id;

            _visibility = Visibility.FriendsOnly;
            _username = user.Username;
            _discriminator = user.Discriminator;
            _friends = new List<ulong>();
            _description = "";
            _achievements = new Dictionary<AchievementID, UserAchievement>();
            _creationDate = DateTime.UtcNow;
            _backgroundColor = System.Drawing.Color.White;
            if (Program.p.rand.Next(0, 10) == 1) // 10% of chance to get this achievement when generating a profile
                ProgressAchievementAsync(AchievementID.ShareAchievement, msg, 0, null).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Create profile from db
        /// </summary>
        /// <param name="json"></param>
        public Profile(ulong id, JObject token)
        {
            _id = id;

            _visibility = (Visibility)token["Visibility"].Value<int>();
            _username = token["Username"].Value<string>();
            _discriminator = token["Discriminator"].Value<string>();
            _friends = token["Friends"].Value<string>().Length > 0 ? token["Friends"].Value<string>().Split(',').Select(x => ulong.Parse(x)).ToList() : new List<ulong>();
            _description = token["Description"].Value<string>();
            _achievements = token["Achievements"].Value<string>().Length > 0 ? token["Achievements"].Value<string>().Split('|').Select((x) =>
            {
                var split = x.Split(',');
                var a_id = (AchievementID)int.Parse(split[0]);
                return new KeyValuePair<AchievementID, UserAchievement>(a_id, new UserAchievement(AchievementList.GetAchievement(a_id), int.Parse(split[1]), split.Skip(2).ToList()));
            }).ToDictionary(x => x.Key, x => x.Value) : new Dictionary<AchievementID, UserAchievement>();
            _creationDate = DateTime.ParseExact(token["CreationDate"].Value<string>(), "yyMMddHHmmss", CultureInfo.InvariantCulture);
            var colorString = token["BackgroundColor"].Value<string>().Split(',');
            _backgroundColor = System.Drawing.Color.FromArgb(int.Parse(colorString[0]), int.Parse(colorString[1]), int.Parse(colorString[2]));
        }

        public MapObject GetProfileToDb(RethinkDB r, bool censorAchievements = false)
        {
            return r.HashMap("id", _id.ToString())
                    .With("Visibility", (int)_visibility)
                    .With("Username", _username)
                    .With("Discriminator", _discriminator)
                    .With("Friends", string.Join(",", _friends))
                    .With("Description", _description)
                    .With("Achievements", string.Join("|", _achievements.Select(x => (int)x.Key + "," + x.Value.ToString(censorAchievements))))
                    .With("CreationDate", _creationDate.ToString("yyMMddHHmmss"))
                    .With("BackgroundColor", _backgroundColor.R + "," + _backgroundColor.G + "," + _backgroundColor.B);
        }

        public System.Drawing.Image GetProfilePicture(IUser user)
        {
            using (HttpClient hc = new HttpClient())
                return System.Drawing.Image.FromStream(hc.GetStreamAsync(user.GetAvatarUrl(ImageFormat.Png)).GetAwaiter().GetResult());
        }

        public void UpdateProfile()
        {
            Program.p.db.UpdateProfile(this);
        }

        public void UpdateProfile(IUser user)
        {
            _username = user.Username;
            _discriminator = user.Discriminator;
            Program.p.db.UpdateProfile(this);
        }

        public bool UpdateDescription(string description)
        {
            if (description.Length > 400)
                return false;
            description = description.Replace("\\n", "\n");
            string tmp = "";
            int i = 0;
            foreach (char c in description)
            {
                if (c == '\n' || i == 47)
                {
                    if (c != '\n') tmp += c;
                    tmp += "\n";
                    i = 0;
                }
                else
                    tmp += c;
                i++;
            }
            if (tmp.Count(x => x == '\n') > 9) // More than 10 lines
                return false;
            _description = tmp;
            Program.p.db.UpdateProfile(this);
            return true;
        }

        public async Task<bool> UpdateColor(string[] color)
        {
            var result = await Features.Tools.Code.SearchColor(color, Program.p.rand);
            if (result.error == Features.Tools.Error.Image.None)
            {
                var c = result.answer.discordColor;
                _backgroundColor = System.Drawing.Color.FromArgb(c.R, c.G, c.B);
                Program.p.db.UpdateProfile(this);
                return true;
            }
            return false;
        }

        public void UpdateVisibility(Visibility v)
        {
            _visibility = v;
            Program.p.db.UpdateProfile(this);
        }

        public async Task AddFriendAsync(Profile p, IUserMessage msg)
        {
            if (_friends.Contains(p._id))
                return;
            p._friends.Add(_id);
            _friends.Add(p._id);
            await ProgressAchievementAsync(AchievementID.AddFriends, msg, _friends.Count, null);
            await p.ProgressAchievementAsync(AchievementID.AddFriends, msg, p._friends.Count, null);
            if (!HaveAchievement(AchievementID.ShareAchievement) && p.HaveAchievement(AchievementID.ShareAchievement))
            {
                await ProgressAchievementAsync(AchievementID.ShareAchievement, msg, 0, null);
                await p.ProgressAchievementAsync(AchievementID.ShareAchievement, msg, 1, null);
            }
            else if (HaveAchievement(AchievementID.ShareAchievement) && !p.HaveAchievement(AchievementID.ShareAchievement))
            {
                await ProgressAchievementAsync(AchievementID.ShareAchievement, msg, 1, null);
                await p.ProgressAchievementAsync(AchievementID.ShareAchievement, msg, 0, null);
            }
            Program.p.db.UpdateProfile(this);
            Program.p.db.UpdateProfile(p);
        }

        public bool RemoveFriend(Profile p)
        {
            if (!_friends.Contains(p._id))
                return false;
            p._friends.Remove(_id);
            _friends.Remove(p._id);
            Program.p.db.UpdateProfile(this);
            Program.p.db.UpdateProfile(p);
            return true;
        }

        public async Task ProgressAchievementAsync(AchievementID id, IUserMessage msg, int progression, string key)
        {
            if (!_achievements.ContainsKey(id))
                _achievements.Add(id, new UserAchievement(AchievementList.GetAchievement(id), 0, new List<string>()));
            var achievement = _achievements[id];
            int oldLevel = achievement.GetLevel();
            achievement.AddProgression(progression, key);
            int newLevel = achievement.GetLevel();
            if (newLevel > oldLevel)
            {
                IEmote emote;
                if (newLevel == 3) emote = new Emoji("🥇"); // Gold
                else if (newLevel == 2) emote = new Emoji("🥈"); // Silver
                else emote = new Emoji("🥉"); // Copper
                await msg?.AddReactionAsync(emote);
                Program.p.db.UpdateProfile(this);
            }
        }

        public List<(System.Drawing.Image, int)> GetAchievements()
        {
            List<(System.Drawing.Image, int)> all = new List<(System.Drawing.Image, int)>();
            foreach (var a in _achievements.OrderBy(x => x.Key))
            {
                if (a.Value.GetLevel() > 0)
                    all.Add((System.Drawing.Image.FromStream(AchievementList.GetAchievementStream(a.Value.GetFilePath())), a.Value.GetLevel()));
            }
            return all;
        }

        public bool HaveAchievement(AchievementID id)
            => _achievements.ContainsKey(id);

        public string GetUsername()
            => _username;

        public string GetDiscriminator()
            => _discriminator;

        public string GetDescription()
            => _description;

        public int GetFriendsCount()
            => _friends.Count;

        public System.Drawing.Color GetBackgroundColor()
            => _backgroundColor;

        public ulong GetId()
            => _id;

        public Visibility GetVisibility()
            => _visibility;

        public bool IsIdFriend(ulong id)
            => _friends.Contains(id);

        private Visibility _visibility;
        private string _username;
        private string _discriminator;
        private List<ulong> _friends;
        private string _description;
        private Dictionary<AchievementID, UserAchievement> _achievements;
        private DateTime _creationDate;
        private System.Drawing.Color _backgroundColor;
        
        private ulong _id;
    }
}
