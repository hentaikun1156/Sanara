﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace SanaraV2.Subscription
{
    public class SubscriptionTags
    {
        private string[] _whitelist;
        private string[] _blacklist;

        public string GetWhitelistTags()
            => _whitelist.Length > 0 ? "`" + string.Join(", ", _whitelist) + "`" : "None";

        public string GetBlacklistTags()
            => _blacklist.Length > 0 ? "`" + string.Join(", ", _blacklist) + "`" : "None";

        public static SubscriptionTags ParseSubscriptionTags(string[] tags)
        {
            List<string> whitelist = new List<string>();
            List<string> blacklist = new List<string>();
            List<string> tagsList = tags.ToList();
            if (tagsList.Contains("full"))
                tagsList.Remove("full");
            else
            {
                foreach (var elem in _defaultBlacklist)
                {
                    foreach (string tag in elem.Value)
                    {
                        blacklist.Add(tag);
                    }
                }
            }
            foreach (string s in tagsList)
            {
                if (string.IsNullOrWhiteSpace(s))
                    continue;
                if (s[0] == '+' || s[0] == '-' || s[0] == '*')
                {
                    string tag = string.Join("", s.Skip(1)).ToLower();
                    if (s[0] == '*')
                    {
                        List<string> toAdd = new List<string>();
                        if (_defaultBlacklist.ContainsKey(tag))
                        {
                            toAdd.AddRange(_defaultBlacklist[tag]);
                        }
                        else
                            toAdd.Add(tag);
                        foreach (string t in toAdd)
                        {
                            if (whitelist.Contains(t))
                                whitelist.Remove(t);
                            if (blacklist.Contains(t))
                                blacklist.Remove(t);
                        }
                    }
                    else
                    {
                        var arr = s[0] == '+' ? whitelist : blacklist;
                        var otherArr = s[0] == '+' ? blacklist : whitelist;
                        List<string> toAdd = new List<string>();
                        if (_defaultBlacklist.ContainsKey(tag))
                        {
                            toAdd.AddRange(_defaultBlacklist[tag]);
                        }
                        else
                            toAdd.Add(tag);
                        foreach (string t in toAdd)
                        {
                            if (otherArr.Contains(t))
                                otherArr.Remove(t);
                            arr.Add(t);
                        }
                    }
                }
                else
                    throw new ArgumentException("Your tag must begin by + or -");
            }
            return new SubscriptionTags
            {
                _whitelist = whitelist.ToArray(),
                _blacklist = blacklist.ToArray()
            };
        }

        public string[] ToStringArray()
        {
            List<string> lists = new List<string>();
            foreach (string s in _whitelist)
                lists.Add("+" + s);
            foreach (string s in _blacklist)
                lists.Add("-" + s);
            return lists.ToArray();
        }

        /// <summary>
        /// Tag must be in whitelist and not in blacklist
        /// </summary>
        /// <returns></returns>
        public bool IsTagValid(string[] tags)
        {
            bool isWhitelisted = _whitelist.Length == 0;
            foreach (string tag in tags)
            {
                if (_blacklist.Contains(tag))
                    return false;
                if (_whitelist.Contains(tag))
                    isWhitelisted = true;
            }
            return isWhitelisted;
        }

        private static Dictionary<string, string[]> _defaultBlacklist = new Dictionary<string, string[]>
        {
            {
                "gore", new[] // Visual brutality
                {
                    "guro", "necrophilia", , "asphyxiation", "snuff"
                }
            },
            {
             
            }
        };
    }
}
