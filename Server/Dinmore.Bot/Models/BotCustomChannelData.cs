using Newtonsoft.Json;
using System;

namespace Dinmore.Bot.Models
{
    [Serializable]
    public class BotCustomChannelData
    {
        [JsonProperty("QnaModelKnowledgeBaseId")]
        public string QnaModelKnowledgeBaseId { get; set; }
    }
}
