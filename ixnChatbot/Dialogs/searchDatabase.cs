using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace ixnChatbot.Dialogs
{
    public class searchDatabase : ComponentDialog
    {
        private readonly luisRecogniser _luisRecogniser;
        
        public searchDatabase(luisRecogniser luisRecogniser)
        {
            _luisRecogniser = luisRecogniser;
            
            var waterfallSteps = new WaterfallStep[]
            {
                intentPrompt,
                intentAnswer,
                searchTable
            };

            AddDialog(new WaterfallDialog("waterfall1", waterfallSteps));
            AddDialog(new TextPrompt("intentPrompt"));

            InitialDialogId = "waterfall1";
        }

        private async Task<DialogTurnResult> intentPrompt(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            if (!_luisRecogniser.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("Luis is not configured correctly."), cancellationToken);
                return await stepContext.NextAsync(null, cancellationToken);
            }
            
            var messageText = stepContext.Options?.ToString() ?? "Hi! Type in a sentence for me to examine";

            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text(messageText) };
            return await stepContext.PromptAsync("intentPrompt", promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> intentAnswer(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var luisResult = await _luisRecogniser.RecognizeAsync<luisResultContainer>(stepContext.Context, cancellationToken);
            switch (luisResult.TopIntent().intent)
            {
                case luisResultContainer.Intent.listProjects:
                        return await stepContext.NextAsync(luisResult.Entities, cancellationToken);
                
                default:
                case luisResultContainer.Intent.None:
                    var promptMessage = "Sorry, I am having trouble understanding you. Please try again";
                    return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
            }
        }
        
        private async Task<DialogTurnResult> searchTable(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            luisResultContainer._Entities entities = (luisResultContainer._Entities) stepContext.Result;
            sqlConnector connector = new sqlConnector();

            string query = connector.selectionQueryBuilder(entities.contactJob, entities.contactName, entities.organizationName,
                entities.projectDevice, entities.projectLocation, entities.projectSkill, entities.projectTitle);
            
            List<List<String>> queryResult = connector.select(query);

            for (int i = 0; i < queryResult.Count; i++)
            {
                List<String> record = queryResult[i];
                string recordData = record[8] + " partnered with " + record[1] + " led by " + record[4];
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(recordData), cancellationToken);
            }
            
            string message = "Please type in another sentence";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, message, cancellationToken);
        }
    } 
}
