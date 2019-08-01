using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PokecordCatcherBot.Services
{
    public class CatcherService : Service
    {
        private readonly PokemonComparer comparer;
        private readonly HttpClient http;

        public CatcherService(PokecordCatcher bot, string hashPath) : base(bot)
        {
            var pokemon = LoadPokemon(hashPath);
            comparer = new PokemonComparer(pokemon);
            Console.WriteLine("Loaded " + pokemon.Select(x => x.Value.Count).Sum() + " pokemon");
            http = new HttpClient();

            Client.MessageReceived += async x => Task.Run(async () => await OnMessage(x))
            .ContinueWith(t => Console.WriteLine(t.Exception.Flatten().InnerException), TaskContinuationOptions.OnlyOnFaulted);
        }

        private async Task OnMessage(SocketMessage msg)
        {
            if (!(msg.Channel is SocketGuildChannel channel))
                return;

            var guild = channel.Guild;

            if (State.WhitelistGuilds && !Configuration.WhitelistedGuilds.Contains(guild.Id))
                return;

            ulong channelId = channel.Id;
            if(State.WhitelistChannels && !Configuration.WhitelistedChannels.Contains(channelId))
                return;

            if (msg.Author.Id != PokecordCatcher.POKECORD_ID || msg.Embeds?.Count == 0)
                return;

            Embed embed = msg.Embeds.First();

            if (embed.Description?.Contains(Configuration.PokecordPrefix + "catch") != true || !embed.Image.HasValue)
                return;

            Console.WriteLine("Detected pokemon, catching...");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            string name = comparer.GetPokemon(await http.GetStreamAsync(embed.Image.Value.Url));

            watch.Stop();

            Console.WriteLine($"Found pokemon in {watch.ElapsedMilliseconds}ms");

            if (State.WhitelistPokemon && !Configuration.WhitelistedPokemon.Any(x => x.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine("Pokemon is not whitelisted, ignoring.");
                Logger.Log($"Ignored a {name} in #{msg.Channel.Name} ({guild.Name})");
                return;
            }

            if (Configuration.CatchMinDelay > 0)
            {
                int delay = Util.rand.Next(Configuration.CatchMinDelay, Configuration.CatchMaxDelay);
                Console.WriteLine($"Delaying for {delay}ms before catching the pokemon...");
                await Task.Delay(delay);
            }

            string lcName = name.ToLower();

            var resp = await ResponseGrabber.SendMessageAndGrabResponse(
                (ITextChannel)msg.Channel,
                $"{Configuration.PokecordPrefix}catch {lcName}",
                x => MessagePredicates.SuccessfulCatchMessage(x, msg, Client.CurrentUser.Id),
                5
            );

            Console.WriteLine(resp == null ? "The Pokecord bot did not respond, catch was a fail." : "Catch confirmed by the Pokecord bot.");

            if (resp != null)
            {
                if (Configuration.EnableCatchResponse)
                    await msg.Channel.SendMessageAsync(Configuration.CatchResponse);

                Logger.Log($"Caught a {name} in #{resp.Channel.Name} ({guild.Name})");
            }
            else
            {
                Logger.Log($"Failed to catch {name} in #{msg.Channel.Name} ({guild.Name})");
            }

            Console.WriteLine();
        }

        private Dictionary<string, List<byte[]>> LoadPokemon(string hashPath)
        {
            var hashes = new Dictionary<string, List<byte[]>>();

            var json = JObject.Parse(File.ReadAllText(hashPath));

            foreach (var x in json)
            {
                var strA = x.Value.Value<JArray>();

                foreach (var s in strA)
                {
                    var str = s.Value<string>().Substring(2);

                    int charsLen = str.Length;
                    byte[] bytes = new byte[charsLen / 2];

                    for (int i = 0; i < charsLen; i += 2)
                        bytes[i / 2] = Convert.ToByte(str.Substring(i, 2), 16);

                    if (hashes.TryGetValue(x.Key, out var val))
                        val.Add(bytes);
                    else
                        hashes.Add(x.Key, new List<byte[]> { bytes });
                }
            }

            return hashes;
        }
    }
}
