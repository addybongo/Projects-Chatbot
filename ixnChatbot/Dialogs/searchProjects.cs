using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ixnChatbot.Cards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace ixnChatbot.Dialogs
{
    public class searchProjects : ComponentDialog
    {
        private readonly luisRecogniser _luisRecogniser;
        private sqlConnector connector;
        private jsonManager jsonManager;
        
        private int SEARCH_RESULT_LIMIT = 4;
        
        public searchProjects(luisRecogniser luisRecogniser)
        {
            _luisRecogniser = luisRecogniser;
            connector = new sqlConnector();
            jsonManager = new jsonManager();
            connector.OpenConnection();

            var waterfallSteps = new WaterfallStep[]
            {
                searchTable, 
                queryTable,
                queryResponse
            };

            AddDialog(new WaterfallDialog("waterfall1", waterfallSteps));
            AddDialog(new TextPrompt("intentPrompt"));

            InitialDialogId = "waterfall1";
        }

        private async Task<DialogTurnResult> searchTable(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            luisResultContainer._Entities entities = (luisResultContainer._Entities) stepContext.Result;

            string query = connector.projectSelectionQueryBuilder(entities.contactJobTitle, entities.contactName, entities.organizationName,
                entities.projectUsages, entities.projectLocation, entities.projectCriteria, entities.projectDescription, entities.organizationOverview);

            List<List<String>> queryResult = connector.select(query);

            if (queryResult.Count == 0)
            {
                string errorMessage = "I'm sorry, I couldn't find any projects matching your parameters. Please try again with different keywords.";
                return await stepContext.ReplaceDialogAsync(InitialDialogId, errorMessage, cancellationToken);
            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text("I found " + queryResult.Count + " related " + (queryResult.Count == 1 ? "project." : "projects.")), cancellationToken);

            for (int i = 0; i < queryResult.Count; i++)
            {
                List<String> record = queryResult[i];

                var projectCard = jsonManager.projectJsonEditor(record[1], record[2], record[3]);
                var response = MessageFactory.Attachment(projectCard);
                await stepContext.Context.SendActivityAsync(response, cancellationToken);
            }
            
            string finishedMessage = "Anything else I can help you with?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, finishedMessage, cancellationToken);
        }

        private async Task<DialogTurnResult> queryTable(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            return null;
        }

        private async Task<DialogTurnResult> queryResponse(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            return null;
        }
    } 
}
