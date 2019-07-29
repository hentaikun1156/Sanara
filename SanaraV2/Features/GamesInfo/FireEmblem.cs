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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SanaraV2.Features.GamesInfo
{
    public static class FireEmblem
    {
        public static async Task<FeatureRequest<Response.Charac, Error.Charac>> SearchCharac(string[] args)
        {
            if (args.Length == 0)
                return new FeatureRequest<Response.Charac, Error.Charac>(null, Error.Charac.Help);
            string thumbnailUrl, name;
            dynamic json;
            using (HttpClient hc = new HttpClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                HttpResponseMessage msg = await hc.GetAsync("https://fireemblem.fandom.com/api/v1/Search/List?query=" + Uri.EscapeDataString(string.Join("%20", args)) + "&limit=1");
                if (msg.StatusCode == HttpStatusCode.NotFound)
                    return new FeatureRequest<Response.Charac, Error.Charac>(null, Error.Charac.NotFound);
                json = JsonConvert.DeserializeObject(await msg.Content.ReadAsStringAsync());
                if (json.items.Count == 0)
                    return new FeatureRequest<Response.Charac, Error.Charac>(null, Error.Charac.NotFound);
                string thumbnailHtml = await hc.GetStringAsync((string)json.items[0].url);
                Match thumbnailMatch = Regex.Match(thumbnailHtml, "<meta property=\"og:image\" content=\"([^\"]+)\"");
                thumbnailUrl = thumbnailMatch.Groups[1].Value;
                name = json.items[0].title;
                json = JsonConvert.DeserializeObject(await hc.GetStringAsync("https://fireemblem.fandom.com/api/v1/Articles/AsSimpleJson?id=" + json.items[0].id));
            }
            return new FeatureRequest<Response.Charac, Error.Charac>(new Response.Charac()
            {
                name = name,
                allCategories = new List<Tuple<string, string>>(),
                thumbnailUrl = thumbnailUrl
            }, Error.Charac.None);
        }
    }
}
