using Newtonsoft.Json;

namespace Dinmore.Api.Models
{
    public class BotCustomChannelData
    {
        [JsonProperty("QnaModelKnowledgeBaseId")]
        public string QnaModelKnowledgeBaseId { get; set; }
    }
}
