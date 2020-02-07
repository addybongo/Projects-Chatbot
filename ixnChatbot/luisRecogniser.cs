using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Extensions.Configuration;

namespace ixnChatbot
{
    public partial class luisRecogniser : IRecognizer
    {
        private readonly LuisRecognizer _recogniser;
        
        public luisRecogniser(IConfiguration configuration)
        {
            //Checks if the appID, apiKey and hostName have been filled out in appsettings.json
            bool isConfigured = !string.IsNullOrEmpty(configuration["LuisAppId"]) && !string.IsNullOrEmpty(configuration["LuisAPIKey"]) && !string.IsNullOrEmpty(configuration["LuisAPIHostName"]);
            
            if (isConfigured)
            {
                var luisApp = new LuisApplication(
                    configuration["LuisAppId"], 
                    configuration["LuisAPIKey"], 
                    "https://" + configuration["LuisAPIHostName"]);
               
                _recogniser = new LuisRecognizer(luisApp);
            }
        }

        public virtual async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
            => await _recogniser.RecognizeAsync(turnContext, cancellationToken);


        public virtual async Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
            => await _recogniser.RecognizeAsync<T>(turnContext, cancellationToken);
    }
}