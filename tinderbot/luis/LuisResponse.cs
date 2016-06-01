using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace tinderbot.luis
{
    public class LuisResponse
    {
        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("intents")]
        public Intents[] Intents { get; set; }

        [JsonProperty("entities")]
        public Entities[] Entities { get; set; }

    }
}