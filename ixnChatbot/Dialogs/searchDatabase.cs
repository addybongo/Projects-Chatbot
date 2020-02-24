using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ixnChatbot.Cards;
using Microsoft.Bot.Builder
    ;
using Microsoft.Bot.Builder.Dialogs;

namespace ixnChatbot.Dialogs
{
    public class searchDatabase : dialogBase
    {
        private projectResultsContainer projectResults;
        private int SEARCH_RESULT_LIMIT = 4; //Number of projects that can be listed at once
        private int searchIndex = 0; //Index of projects currently being listen
        
        public searchDatabase(luisRecogniser luisRecogniser) : base(luisRecogniser, nameof(searchDatabase))
        {
            _luisRecogniser = luisRecogniser;

            var waterfallSteps = new WaterfallStep[]
            {
                intentPrompt,
                intentAnswer
            };

            AddDialog(new WaterfallDialog("waterfall1", waterfallSteps));
            AddDialog(new TextPrompt("intentPrompt"));
            AddDialog(new searchProject(luisRecogniser));

            InitialDialogId = "waterfall1";
        }

        private async Task<DialogTurnResult> intentPrompt(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            if (!_luisRecogniser.IsConfigured)
            {
                sendMessage(stepContext, "LUIS is not configured correctly!", cancellationToken);
            }
            
            var messageText = stepContext.Options?.ToString() ?? "Hi! What are you looking for?";

            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text(messageText) };
            return await stepContext.PromptAsync("intentPrompt", promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> intentAnswer(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            string response = (string) stepContext.Result;
            
            if (response == "selectProject")
            {
                DialogTurnResult returnContext = await stepContext.BeginDialogAsync(nameof(searchProject), stepContext.Result, cancellationToken);
                stepContext.Context.Activity.Text = (string) returnContext.Result;
            }
            
            var luisResult = await _luisRecogniser.RecognizeAsync<luisResultContainer>(stepContext.Context, cancellationToken);
            luisResultContainer._Entities entities = luisResult.Entities;

            switch (luisResult.TopIntent().intent)
            {
                case luisResultContainer.Intent.listProjects:
                    searchIndex = 0;
                    string query = connector.projectSelectionQueryBuilder(entities.contactJobTitle, entities.contactName, entities.organizationName,
                        entities.projectUsages, entities.projectLocation, entities.projectCriteria, entities.projectDescription, entities.organizationOverview);
                    projectResultsContainer tempProjectResults = new projectResultsContainer(connector.select(query), connector.getFieldNames("Projects"));
                    
                    if (tempProjectResults.getNumberOfRecords() == 0)
                    {
                        sendMessage(stepContext, "I'm sorry, I couldn't find any projects matching your parameters. Please try again with different keywords.", cancellationToken);
                        break;
                    }
                    projectResults = tempProjectResults;
                    
                    sendMessage(stepContext, "I found " + projectResults.getNumberOfRecords() + " related " + 
                                (projectResults.getNumberOfRecords() == 1 ? "project." : "projects. ") + "Here are the top 4 results: ", cancellationToken);
                    await displayProjects(stepContext, cancellationToken);
                    break;
                
                case luisResultContainer.Intent.displayMoreProjects:
                    searchIndex += 4;
                    if (projectResults != null)
                    {
                        sendMessage(stepContext, "Here are some more results for your last search.", cancellationToken);
                        await displayProjects(stepContext, cancellationToken);
                    }
                    else
                    {
                        sendMessage(stepContext, "There is no projects to show! Please search for projects first.", cancellationToken);
                    }
                    break;
                
                default:
                    var promptMessage = "I'm sorry, I am having trouble understanding you. What would you like me to do?";
                    return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
            }
            return await stepContext.ReplaceDialogAsync(InitialDialogId, "Could I help you with anything else?", cancellationToken);
        }

        private async Task displayProjects(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            for (int i = 0; i < SEARCH_RESULT_LIMIT; i++)
            {
                int projectIndex = searchIndex + i;
                var projectCard = jsonManager.projectJsonEditor(projectResults.getValue(projectIndex, 1),
                    projectResults.getValue(projectIndex, 2), projectResults.getValue(projectIndex, 3));
                var response = MessageFactory.Attachment(projectCard);
                await stepContext.Context.SendActivityAsync(response, cancellationToken);
            }
        }
    } 
}
