using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Dinmore.Bot.Dialogs;
using System.Configuration;
using Dinmore.Bot.Models;
using Newtonsoft.Json;

namespace Dinmore.Bot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                if (activity.ChannelId == "directline")
                {
                    // Extract the QnAKnowledgeBaseId from the channeldata - which is derived from the device at each exhibit
                    var customChannelData = JsonConvert.DeserializeObject<BotCustomChannelData>(activity.ChannelData.ToString());
                    await Conversation.SendAsync(activity, () => new QnARootDialog(knowledgebaseId: customChannelData?.QnaModelKnowledgeBaseId, subscriptionKey: ConfigurationManager.AppSettings["QnASubscriptionKey"]));
                }
                else
                {
                    await Conversation.SendAsync(activity, () => new RootDialog());
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}