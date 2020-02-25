using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ixnChatbot.Cards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace ixnChatbot.Dialogs
{
    public class searchProject : dialogBase
    {
        private int SEARCH_RESULT_LIMIT = 4;
        
        public searchProject(luisRecogniser luisRecogniser) : base(luisRecogniser, nameof(searchProject))
        {
            jsonManager = new jsonManager();

            var waterfallSteps = new WaterfallStep[]
            {
                partOne,
                partTwo
            };

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> partOne(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            string query = projectQueryCreator(stepContext.Options.ToString());
            
            string[] projectRecord = new projectResultsContainer(connector.select(query), 
                connector.getFieldNames("Projects")).getRecord(0);

            Attachment projectCard =  jsonManager.detailedProjectCardGenerator(projectRecord[8], projectRecord[1], projectRecord[17],
                projectRecord[4], projectRecord[16]);
            var response = MessageFactory.Attachment(projectCard);
            
            await stepContext.Context.SendActivityAsync(response, cancellationToken);

            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("What would you like to know about this project?") };
            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
        }
        
        private async Task<DialogTurnResult> partTwo(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var luisResult = await _luisRecogniser.RecognizeAsync<luisResultContainer>(stepContext.Context, cancellationToken);
            luisResultContainer._Entities entities = luisResult.Entities;

            switch (luisResult.TopIntent().intent)
            {
                case luisResultContainer.Intent.listProjects:
                    return await stepContext.EndDialogAsync(stepContext.Result, cancellationToken);
                default:
                    return await stepContext.ReplaceDialogAsync(nameof(WaterfallDialog), stepContext.Result, cancellationToken);
            }
        }

        private string projectQueryCreator(string activityValue)
        {
            dynamic jsonObj = JsonConvert.DeserializeObject(activityValue);
            string id = jsonObj["data"];

            return "SELECT * FROM PROJECTS WHERE projectID = " + id + ";";
        }
    } 
}
