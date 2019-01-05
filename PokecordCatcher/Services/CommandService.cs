using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using PokecordCatcherBot.Attributes;
using PokecordCatcherBot.Models;

namespace PokecordCatcherBot.Services
{
    public class CommandService : Service
    {
        private readonly List<MethodInfo> commandMethods;

        public CommandService(PokecordCatcher bot) : base(bot)
        {
            commandMethods = FindCommandMethods();

            Client.MessageReceived += OnMessage;
        }

        private async Task OnMessage(SocketMessage msg)
        {
            if (!msg.Content.StartsWith(Configuration.UserbotPrefix) || msg.Author.Id != Configuration.OwnerID)
                return;

            var args = msg.Content.Split(' ').ToList();
            var commandName = args[0].Substring(Configuration.UserbotPrefix.Length);
            args.RemoveAt(0);

            var command = commandMethods.FirstOrDefault(x => String.Equals(x.GetCustomAttribute<CommandAttribute>().Name, commandName, StringComparison.OrdinalIgnoreCase));

            if (command != null)
            {
                try
                {
                    var cmdTask = (Task)command.Invoke(this, new object[] { msg, args.ToArray() });

                    var task = Task.Run(async () => await cmdTask)
                        .ContinueWith(t => Console.WriteLine(t.Exception.Flatten().InnerException), TaskContinuationOptions.OnlyOnFaulted);
                }
                catch (Exception e) { Console.WriteLine(e); }
            }
        }

        private List<MethodInfo> FindCommandMethods() =>
            typeof(PokecordCatcher).Assembly.GetTypes()
                .SelectMany(x => x.GetMethods())
                .Where(x => x.GetCustomAttribute(typeof(CommandAttribute)) != null).ToList();

        [Command(nameof(Status), "Displays information about the bot's state.")]
        public async Task Status(SocketMessage msg, string[] args)
        {
            var props = typeof(State).GetProperties();
            await msg.Channel.SendMessageAsync($"```{String.Join('\n', props.Select(x => $"{x.Name}: {x.GetValue(State)}"))}```");
        }

        [Command(nameof(Reload), "Reload the bot's configuration.")]
        public async Task Reload(SocketMessage msg, string[] args)
        {
            bot.UpdateConfiguration("config.json");
            await msg.Channel.SendMessageAsync("Configuration reloaded.");
        }

        [Command(nameof(ToggleGuilds), "Toggle guild whitelisting.")]
        public async Task ToggleGuilds(SocketMessage msg, string[] args)
        {
            State.WhitelistGuilds = !State.WhitelistGuilds;
            File.WriteAllText("state.data", JsonConvert.SerializeObject(State));
            await msg.Channel.SendMessageAsync("Whitelisting of guilds has been toggled to " + State.WhitelistGuilds);
        }

        [Command(nameof(ToggleChannels), "Toggle channel whitelisting.")]
        public async Task ToggleChannels(SocketMessage msg, string[] args)
        {
            State.WhitelistChannels = !State.WhitelistChannels;
            File.WriteAllText("state.data", JsonConvert.SerializeObject(State));
            await msg.Channel.SendMessageAsync("Whitelisting of channels has been toggled to " + State.WhitelistChannels);
        }

        [Command(nameof(TogglePokemon), "Toggle pokemon whitelisting.")]
        public async Task TogglePokemon(SocketMessage msg, string[] args)
        {
            State.WhitelistPokemon = !State.WhitelistPokemon;
            File.WriteAllText("state.data", JsonConvert.SerializeObject(State));
            await msg.Channel.SendMessageAsync("Whitelisting of pokemon has been toggled to " + State.WhitelistPokemon);
        }

        [Command(nameof(ToggleSpam), "Toggle pokemon whitelisting.")]
        public async Task ToggleSpam(SocketMessage msg, string[] args)
        {
            State.SpammerEnabled = !State.SpammerEnabled;
            File.WriteAllText("state.data", JsonConvert.SerializeObject(State));
            await msg.Channel.SendMessageAsync("Spam has been toggled to " + State.SpammerEnabled);
        }

        [Command(nameof(Echo), "Has the bot say something.")]
        public async Task Echo(SocketMessage msg, string[] args) => 
            await msg.Channel.SendMessageAsync(String.Join(' ', args));

        [Command(nameof(Display), "Displays all pokemon of a certain name.")]
        public async Task Display(SocketMessage msg, string[] args) => 
            await msg.Channel.SendMessageAsync($"{Configuration.PokecordPrefix}pokemon --name {String.Join(' ', args)}");

        [Command(nameof(DisplayAll), "Displays all pokemon.")]
        public async Task DisplayAll(SocketMessage msg, string[] args) => 
            await msg.Channel.SendMessageAsync($"{Configuration.PokecordPrefix}pokemon");

        [Command(nameof(Details), "Toggles showing of detailed pokemon stats.")]
        public async Task Details(SocketMessage msg, string[] args) => 
            await msg.Channel.SendMessageAsync($"{Configuration.PokecordPrefix}detailed");

        [Command(nameof(Exit), "Exits the userbot program.")]
        public async Task Exit(SocketMessage msg, string[] args)
        {
            await Client.LogoutAsync();
            Environment.Exit(0);
        }
        
        [Command(nameof(StartTrade), "Starts a trade with the bot owner.")]
        public async Task StartTrade(SocketMessage msg, string[] args) => 
            await msg.Channel.SendMessageAsync($"{Configuration.PokecordPrefix}trade <@{Configuration.OwnerID}>");
            
        [Command(nameof(Accept), "Runs accept command")]
        public async Task Accept(SocketMessage msg, string[] args) => 
            await msg.Channel.SendMessageAsync($"{Configuration.PokecordPrefix}accept");
            
        [Command(nameof(AddId), "Adds a list of specified IDs")]
        public async Task AddId(SocketMessage msg, string[] args) => 
            await msg.Channel.SendMessageAsync($"{Configuration.PokecordPrefix}p add {String.Join(' ', args)}");

        [Command(nameof(Confirm), "Confirms the current trade.")]
        public async Task Confirm(SocketMessage msg, string[] args) => 
            await msg.Channel.SendMessageAsync($"{Configuration.PokecordPrefix}confirm");

        [Command(nameof(Trade), "Trades all pokemon a certain name.")]
        public async Task Trade(SocketMessage msg, string[] args)
        {
            var list = await ResponseGrabber.SendMessageAndGrabResponse(
                (ITextChannel)msg.Channel,
                $"{Configuration.PokecordPrefix}pokemon --name {String.Join(' ', args)}",
                x => MessagePredicates.PokemonListingMessage(x, msg),
                5
            );

            await Task.Delay(2000);

            if (list == null)
            {
                await msg.Channel.SendMessageAsync("Pokecord didn't display pokemon, aborting.");
                return;
            }

            var pokemans = Util.ParsePokemonListing(list.Embeds.First().Description);

            await Task.Delay(1500);

            var trade = await ResponseGrabber.SendMessageAndGrabResponse(
                (ITextChannel)msg.Channel,
                $"{Configuration.PokecordPrefix}trade <@{Configuration.OwnerID}>",
                x => MessagePredicates.TradeMessage(x, msg),
                5
            );

            await Task.Delay(2000);

            if (trade == null)
            {
                await msg.Channel.SendMessageAsync("Pokecord didn't create trade, aborting.");
                return;
            }

            await msg.Channel.SendMessageAsync($"{Configuration.PokecordPrefix}p add {String.Join(' ', pokemans.Select(x => x.Id))}");
            await Task.Delay(1500);
            await msg.Channel.SendMessageAsync($"{Configuration.PokecordPrefix}confirm");
            await Task.Delay(1500);
        }

        [Command(nameof(TradeById), "Trades a pokemon with a certain ID.")]
        public async Task TradeById(SocketMessage msg, string[] args)
        {
            if (args.Length != 1)
            {
                await msg.Channel.SendMessageAsync("Invalid arguments. Please supply an ID.");
                return;
            }

            var trade = await ResponseGrabber.SendMessageAndGrabResponse(
                (ITextChannel)msg.Channel,
                $"{Configuration.PokecordPrefix}trade <@{Configuration.OwnerID}>",
                x => MessagePredicates.TradeMessage(x, msg),
                5
            );

            await Task.Delay(2000);

            if (trade == null)
            {
                await msg.Channel.SendMessageAsync("Pokecord didn't create trade, aborting.");
                return;
            }

            await msg.Channel.SendMessageAsync($"{Configuration.PokecordPrefix}p add {args[0]}");
            await Task.Delay(1500);
            await msg.Channel.SendMessageAsync($"{Configuration.PokecordPrefix}confirm");
            await Task.Delay(1500);
        }
    }
}
