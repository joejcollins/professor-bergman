using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.CognitiveServices.QnAMaker;

namespace Dinmore.Bot.Dialogs
{
    [Serializable]
    public class QnARootDialog : QnAMakerDialog
    {
        public QnARootDialog(string subscriptionKey, string knowledgebaseId) : base(new QnAMakerService(new QnAMakerAttribute(subscriptionKey, knowledgebaseId, "No good match in FAQ.", 0.5)))
        {

        }
    }
}