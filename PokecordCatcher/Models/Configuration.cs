using System;
using System.Collections.Generic;
using System.Text;

namespace PokecordCatcherBot.Models
{
    public class Configuration
    {
        public string Token { get; set; }
        public string PokecordPrefix { get; set; }
        public string UserbotPrefix { get; set; }
        public ulong OwnerID { get; set; }
        public bool EnableLogging { get; set; }

        public bool EnableCatchResponse { get; set; }
        public string CatchResponse { get; set; }

        public int CatchMinDelay { get; set; }
        public int CatchMaxDelay { get; set; }
        
        public ulong SpamChannelID { get; set; }
        public int SpammerMinDelay { get; set; }
        public int SpammerMaxDelay { get; set; }
        public string[] SpammerMessages { get; set; }

        public string[] WhitelistedPokemon { get; set; }
        public ulong[] WhitelistedChannels { get; set; }
        public ulong[] WhitelistedGuilds { get; set; }
    }
}
