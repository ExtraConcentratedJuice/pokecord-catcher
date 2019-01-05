using System;
using System.Collections.Generic;
using System.Text;

namespace PokecordCatcherBot.Models
{
    public class State
    {
        public bool WhitelistChannels { get; set; }
        public bool WhitelistGuilds { get; set; }
        public bool WhitelistPokemon { get; set; }
        public bool SpammerEnabled { get; set; }
    }
}
