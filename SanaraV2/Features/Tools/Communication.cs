// This file is part of Sanara.
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

using System.Threading.Tasks;
using WebSocketSharp;

namespace SanaraV2.Features.Tools
{
    public static class Communication
    {
        public static async Task<FeatureRequest<Response.Autocomplete, Error.Autocomplete>> Autocomplete(string[] args)
        {
            System.Console.WriteLine("OK");
            if (args.Length == 0)
                return new FeatureRequest<Response.Autocomplete, Error.Autocomplete>(null, Error.Autocomplete.Help);
            try
            {
                using (var ws = new WebSocket("wss://api.talktotransformer.com/"))
                {
                    ws.Compression = CompressionMethod.Deflate;
                    ws.Log.Level = LogLevel.Trace;

                    ws.OnMessage += (sender, e) =>
                    {
                        System.Console.WriteLine(e.Data);
                    };

                    ws.OnOpen += (sender, e) =>
                    {
                        ws.Send("{\"id\":\"0\",\"type\":\"sample\",\"data\":{\"text\":\"" + string.Join(" ", args) + "\",\"captchaToken\":\"03AOLTBLT9LPrsJxHb_thgyrkzfaMBPYPffhWI5Yl-M2Y0zl3USpn8jNrmVmJH6WL9DVechb8ExSjQF38R_XBzrLjGE9BYFnE4ar4AYBmYyZ1XLgqYdmjQH12CjcQmTLkAOpD_8greuRpOI74ssqOcG4ddC0TKxNWK8LNhuAtnNUwDi4bYdmCqs7s_Ph0m-jkGw8meq5_9OO4zahTcys54PFMShaXeOJPaDL-aw5jeVWNpnjhHZoSWdrgPwsbQRG-JIa7p17toFZ4WH7vc9V4Zc50NX4gBpJAuKlHw3aPLQqmmOZRLqzIkTwTgCLdif8RKor0o_ui8AcFo2c5VFuK1YoMjaHTd4wEyHQ\"}}");
                    };

                    ws.Connect();
                  }
            } catch (System.Exception e)
            {
                System.Console.WriteLine(e.Message);
            }
            while (true) { }
        }
    }
}
