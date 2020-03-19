using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;

namespace ixnChatbot.Dialogs
{
    public class dialogBase : ComponentDialog
    {
        protected luisRecogniser _luisRecogniser;
        protected sqlConnector connector;

        public dialogBase(luisRecogniser luisRecogniser, string dialogID) : base(dialogID)
        {
            _luisRecogniser = luisRecogniser;
            connector = new sqlConnector();

            connector.OpenConnection();
        }
        
        protected async void sendMessage(WaterfallStepContext stepContext, string message, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(message), cancellationToken);
        }
        
        protected string resolveCardActions(string actionText)
        {
            dynamic jsonObj = JsonConvert.DeserializeObject(actionText);
            if (jsonObj["id"] == "selectProject")
            {
                return "selectProject";
            }
            else
            {
                {
                    return "somethingElse";
                }
            }
        }
    }
}