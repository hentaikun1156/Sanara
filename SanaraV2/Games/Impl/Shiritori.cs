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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SanaraV2.Features.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SanaraV2.Games.Impl
{
    public class ShiritoriPreload : APreload
    {
        public ShiritoriPreload() : base(new[] { "shiritori" }, 15, Sentences.ShiritoriGame)
        { }

        public override bool IsNsfw()
            => false;

        public override bool DoesAllowFull()
            => false;

        public override bool DoesAllowSendImage()
            => false;

        public override bool DoesAllowCropped()
            => false;

        public override Shadow DoesAllowShadow()
            => Shadow.None;

        public override Multiplayer DoesAllowMultiplayer()
            => Multiplayer.Both;

        public override MultiplayerType GetMultiplayerType()
            => MultiplayerType.Elimination;

        public override string GetRules(ulong guildId, bool isMultiplayer)
            => (isMultiplayer ? Sentences.RulesShiritoriMulti(guildId) : Sentences.RulesShiritori(guildId)) + Environment.NewLine + Sentences.RulesShiritori2(guildId);
    }

    public class Shiritori : AGame
    {
        public Shiritori(ITextChannel chan, Config config, ulong playerId) : base(chan, Constants.shiritoriDictionnary, config, playerId)
        { }

        protected override void Init()
        {
            _alreadySaid = new List<string>();
            _currWord = null;
        }

        protected override async Task<string[]> GetPostAsync()
        {
            if (HaveMultiplayerLobby())
                return new string[] { null };
            return new[] { await GetPostSolo() };
        }

        private async Task<string> GetPostSolo()
        {
            if (_currWord == null) // The bot start the game by saying しりとり
            {
                _currWord = "しりとり";
                _dictionnary.Remove(_dictionnary.Find(x => x.Split('$')[0] == _currWord));
                _alreadySaid.Add("しりとり");
                return "しりとり (shiritori)";
            }
            string[] validWords = GetValidWords();
            if (validWords.Length == 0) // Not supposed to happen
            {
                LooseException le = new LooseException(GetStringFromSentence(Sentences.ShiritoriNoWord));
                await Program.p.LogError(new LogMessage(LogSeverity.Error, le.Source, le.Message, le));
                throw le;
            }
            string word = validWords[Program.p.rand.Next(0, validWords.Length)];
            string[] splitWord = word.Split('$');
            _dictionnary.Remove(word);
            _alreadySaid.Add(splitWord[0]);
            _currWord = Linguist.ToHiragana(splitWord[0]);
            return splitWord[0] + " (" + Linguist.ToRomaji(splitWord[0]) + ") - " + GetStringFromSentence(Sentences.Meaning) + ": " + splitWord[1];
        }

        protected override PostType GetPostType()
            => PostType.Text;

        protected override bool CongratulateOnGuess()
            => false;

        protected override string Help()
            => null;

        protected override async Task<string> GetCheckCorrectAsync(string userAnswer)
        {
            string hiraganaAnswer = ReplaceLocalString(Linguist.ToHiragana(userAnswer));
            if (_currWord == null) // Multiplayer only
            {
                if (hiraganaAnswer != "しりとり")
                    return GetStringFromSentence(Sentences.ShiritoriExplainBegin);
                _currWord = "しりとり";
                _alreadySaid.Add(_currWord);
                _dictionnary.Remove(_dictionnary.Find(x => x.Split('$')[0] == _currWord));
                return null;
            }
            if (hiraganaAnswer.Any(c => c < 0x0041 || (c > 0x005A && c < 0x0061) || (c > 0x007A && c < 0x3041) || (c > 0x3096 && c < 0x30A1) || c > 0x30FA))
                return GetStringFromSentence(Sentences.OnlyHiraganaKatakanaRomaji);
            dynamic json;
            using (HttpClient hc = new HttpClient())
                json = JsonConvert.DeserializeObject(await hc.GetStringAsync("http://www.jisho.org/api/v1/search/words?keyword=" + Uri.EscapeDataString(userAnswer)));
            if (json.data.Count == 0)
                return GetStringFromSentence(Sentences.ShiritoriDoesntExist);
            bool isCorrect = false, isNoun = false;
            string reading;
            string[] meanings = new string[] { };
            foreach (dynamic s in json.data)
            {
                foreach (dynamic jp in s.japanese)
                {
                    reading = Linguist.ToHiragana((string)jp.reading);
                    if (reading == null)
                        continue;
                    reading = ReplaceLocalString(reading);
                    if (reading == hiraganaAnswer)
                    {
                        isCorrect = true;
                        foreach (dynamic meaning in s.senses)
                        {
                            foreach (dynamic partSpeech in meaning.parts_of_speech)
                            {
                                if (partSpeech == "Noun")
                                {
                                    isNoun = true;
                                    meanings = ((JArray)meaning.english_definitions).Select(x => (string)x).ToArray();
                                    goto ContinueCheck;
                                }
                            }
                        }
                    }
                }
            }
        ContinueCheck:
            if (!isCorrect)
                return GetStringFromSentence(Sentences.ShiritoriDoesntExist);
            string lastCharac = GetLastCharacter(_currWord);
            if (!hiraganaAnswer.StartsWith(ReplaceLocalString(GetLastCharacter(_currWord))))
                return Sentences.ShiritoriMustBegin(GetGuildId(), lastCharac, Linguist.ToRomaji(lastCharac));
            if (!isNoun)
                return GetStringFromSentence(Sentences.ShiritoriNotNoun);
            if (GetLastCharacter(hiraganaAnswer) == hiraganaAnswer)
                return GetStringFromSentence(Sentences.ShiritoriTooSmall);
            if (_alreadySaid.Contains(hiraganaAnswer))
            {
                await LooseAsync(GetStringFromSentence(Sentences.ShiritoriAlreadySaid));
                return "";
            }
            if (hiraganaAnswer.Last() == 'ん')
            {
                await LooseAsync(GetStringFromSentence(Sentences.ShiritoriEndWithN));
                return "";
            }
            var elem = _dictionnary.Find(x => x.Split('$')[0] == hiraganaAnswer);
            if (HaveMultiplayerLobby())
                _endTurnMsg = hiraganaAnswer + " (" + Linguist.ToRomaji(hiraganaAnswer) + ") - " + GetStringFromSentence(Sentences.Meaning) + ": " + string.Join(", ", meanings.Select(x => "\"" + x + "\""));
            _dictionnary.Remove(elem);
            _alreadySaid.Add(hiraganaAnswer);
            _currWord = hiraganaAnswer;
            return null;
        }

        private string ReplaceLocalString(string input)
            => input.Replace('ぢ', 'じ'); // We do that because both characters are pronounced the same way

        protected override async Task<string> GetLoose()
        {
            if (_currWord == null) // Multiplayer, if nobody say anything
                return Sentences.ShiritoriExplainBegin(GetGuildId());
            string[] validWords = GetValidWords();
            if (validWords.Length == 0)
                return GetStringFromSentence(Sentences.ShiritoriNoMoreWord);
            string word = validWords[Program.p.rand.Next(0, validWords.Length)];
            string[] splitWord = word.Split('$');
            return Sentences.ShiritoriSuggestion(GetGuildId(), splitWord[0], Linguist.ToRomaji(splitWord[0]), splitWord[1]);
        }

        protected override string AnnounceNextTurnInternal()
            => Environment.NewLine + _endTurnMsg;

        private string[] GetValidWords()
            => _dictionnary.Where(x => x.StartsWith(GetLastCharacter(_currWord))).ToArray(); // Sanara word must begin by the ending of the player word

        private string GetLastCharacter(string word)
        {
            char lastChar = word.Last();
            if (lastChar == 'ゃ' || lastChar == 'ぃ' || lastChar == 'ゅ'
                || lastChar == 'ぇ' || lastChar == 'ょ')
                return (word.Substring(word.Length - 2, 2));
            return (lastChar.ToString());
        }

        private List<string>    _alreadySaid; // We make sure that the user don't say the same word twice
        private string          _currWord; // The current word

        public static List<string> LoadDictionnary()
        {
            if (!File.Exists("Saves/shiritoriWords.dat"))
                return (new List<string>());
            return (File.ReadAllLines("Saves/shiritoriWords.dat").ToList());
        }

        private string _endTurnMsg;
    }
}
