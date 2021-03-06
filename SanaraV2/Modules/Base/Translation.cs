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
using System;
using System.Linq;

namespace SanaraV2.Modules.Base
{
    public static class Translation
    {
        public struct TranslationData
        {
            public TranslationData(string language, string content)
            {
                this.language = language;
                this.content = content;
            }

            public string language;
            public string content;
        }

        public static string GetTranslation(ulong guildId, string id, params string[] args)
        {
            string language = "en";
            if (guildId != 0)
                language = Program.p.db.Languages[guildId];
            if (Program.p.translations.ContainsKey(id))
            {
                TranslationData value = Program.p.translations[id].Find(x => x.language == language);
                string elem;
                if (value.language != null)
                    elem = value.content;
                else if (Program.p.translations[id].Any(x => x.language == "en"))
                    elem = Program.p.translations[id].Find(x => x.language == "en").content;
                else
                    return "An error occured in the translation submodule: The id " + id + " doesn't exist.";
                for (int i = 0; i < args.Length; i++)
                {
                    elem = elem.Replace("{" + i + "}", args[i]);
                }
                elem = elem.Replace("\\n", Environment.NewLine);
                return elem;
            }
            return "An error occured in the translation submodule: The id " + id + " doesn't exist.";
        }
    }
}
