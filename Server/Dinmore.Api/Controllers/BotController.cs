using dinmore.api.Interfaces;
using dinmore.api.Models;
using Dinmore.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dinmore.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/Bot")]
    public class BotController : Controller
    {
        private readonly IStoreRepository _storeRepository;
        private readonly AppSettings _appSettings;

        public BotController(IOptions<AppSettings> appSettings, IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;
            _appSettings = appSettings.Value;
        }

        /// <summary>
        /// Returns an array of all devices in the storage table
        /// </summary>
        /// <returns>An array of all devices in the storage table</returns>
        [HttpGet]
        public async Task<IActionResult> Get(string conversationId)
        {
            if (string.IsNullOrEmpty(conversationId)) return BadRequest();

            string result = $"Sorry no response for {conversationId}";

            DirectLineClient directLine = new DirectLineClient(_appSettings.BotDirectLineSecret);
            directLine.BaseUri = new Uri(_appSettings.DirectLineBaseUrl);

            try
            {
                var botResponse = await directLine.Conversations.GetActivitiesAsync(conversationId); //, watermark);

                // Now check if we have some messages we've not seen already, if so iterate through them
                if (botResponse?.Activities?.Count > 0)
                {
                    var activitiesFromBot = from x in botResponse.Activities
                                            where x.From.Name == _appSettings.BotId
                                            select x;

                    // Not using watermark here, so if more than one get the last one
                    var activity = activitiesFromBot.LastOrDefault();
                    result = activity.Text;
                }
            }
            catch (Exception exp)
            {
                // oh boy
            }

            return Ok(result);
        }


        [HttpPost]
        public async Task<IActionResult> Post(string deviceId, string message)
        {
            //get the device from storage based on the device id
            var device = await _storeRepository.GetDevice(deviceId);
            if (device == null) return BadRequest();

            DirectLineClient directLine = new DirectLineClient(_appSettings.BotDirectLineSecret);

            if (device.QnAKnowledgeBaseId == null)
            {
                return Ok("QnA KnowledgeBase not found for given device Id - please check configuration");
            }

            var conversation = await directLine.Conversations.StartConversationAsync();
//            conversation.

            var channelData = new BotCustomChannelData { QnaModelKnowledgeBaseId = device.QnAKnowledgeBaseId };

            // Send a new activity to the bot with the captured text to process
            Activity activity = new Activity
            {
                Text = message,
                From = new ChannelAccount(_appSettings.BotFromUserName),
                Type = ActivityTypes.Message,
                ChannelData = channelData
            };

            
            var botResponse = await directLine.Conversations.PostActivityAsync(conversation.ConversationId, activity);

            // TODO: Investigate conversation Id format being returned
            var res = botResponse.Id?.Replace("|0000000", "");
            return Ok(res);
        }
    }
}