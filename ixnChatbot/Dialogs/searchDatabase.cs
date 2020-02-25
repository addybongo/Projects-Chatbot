using System;
using System.Collections.Generic;
using System.Net.Mime;
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
                cardOrUser,
                intentAnswer
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new searchProject(luisRecogniser));

            InitialDialogId = nameof(WaterfallDialog);
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
            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
        }

        //This step is needed to split up messages that came from a card action, or a typed message. It allows us to add
        //another dialog to the stack that focuses on a specific project a user clicked.
        private async Task<DialogTurnResult> cardOrUser(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            //Message Code sent if user clicks on a card
            if (stepContext.Result.ToString() == "#AC_SP")
            {
                return await stepContext.BeginDialogAsync(nameof(searchProject), stepContext.Context.Activity.Value, cancellationToken);
            }
            //Proceed to LUIS if the message was typed by user...
            return await stepContext.NextAsync(stepContext.Result, cancellationToken);
        }


        //Responsible for resolving user language via LUIS
        private async Task<DialogTurnResult> intentAnswer(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
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
            //If we have less records than the search result limit, set the number of cards to make to what is available
            int numberOfSearchResults = projectResults.getNumberOfRecords() < SEARCH_RESULT_LIMIT
                ? projectResults.getNumberOfRecords()
                : SEARCH_RESULT_LIMIT;

            sendMessage(stepContext, "I found " + projectResults.getNumberOfRecords() + " related " + 
                                     (projectResults.getNumberOfRecords() == 1 ? "project. " : "projects. Here are the top results:") , cancellationToken);
            
            for (int i = 0; i < numberOfSearchResults; i++)
            {
                string[] currentRecord = projectResults.getRecord(i);
                
                int projectIndex = searchIndex + i;
                var projectCard = jsonManager.projectCardGenerator( currentRecord[0], currentRecord[1], 
                    currentRecord[2], currentRecord[3]);
                var response = MessageFactory.Attachment(projectCard);
                await stepContext.Context.SendActivityAsync(response, cancellationToken);
            }
        }
    } 
}
