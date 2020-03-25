using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder
    ;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;

namespace ixnChatbot.Dialogs
{
    public class searchDatabase : dialogBase
    {
        private readonly int SEARCH_RESULT_LIMIT = 4; //Number of projects that can be listed at once
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
                dynamic jsonObj = JsonConvert.DeserializeObject(stepContext.Context.Activity.Value.ToString());
                int id = jsonObj["data"];
                
                return await stepContext.BeginDialogAsync(nameof(searchProject), projectResults.getProjectByID(id), cancellationToken);
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
                    if (!checkForEntities(entities))
                    {
                        sendMessage(stepContext, "I'm sorry, please specify a criteria for me to search for, such as" +
                                                 " keywords or names.", cancellationToken);
                        break;
                    }
                    
                    searchIndex = 0;
                    sendMessage(stepContext, "Please bare with me, I am searching for projects that match your criteria", cancellationToken);
                    projectBundle searchResult = new projectBundle(entities);
                    
                    if (searchResult.getNumberOfProjects() == 0)
                    {
                        sendMessage(stepContext, "I'm sorry, I couldn't find any projects matching your parameters. Please try again with different keywords.", cancellationToken);
                        break;
                    }
                    
                    await displayProjects(searchResult, stepContext, cancellationToken);
                    break;
                
                case luisResultContainer.Intent.displayMoreProjects:
                    searchIndex += 4;
                    if (projectResults == null)
                    {
                        sendMessage(stepContext, "There is no projects to show! Please search for projects first.", cancellationToken);
                        break;
                    }

                    if (searchIndex >= projectResults.getNumberOfProjects())
                    {
                        sendMessage(stepContext, "There are no more projects to show from this search! Here are the last 4 projects.", cancellationToken);
                        searchIndex -= 4;
                    }
                    else sendMessage(stepContext, "Here are some more results for your last search.", cancellationToken);
                    
                    
                    for (int i = 0; i < projectResults.getNumberOfProjects() - searchIndex; i++)
                    {
                        Project currentRecord = projectResults.getProject(i + searchIndex);
                        var response = MessageFactory.Attachment(currentRecord.getSimplePatientCard());
                        await stepContext.Context.SendActivityAsync(response, cancellationToken);
                    }

                    break;
                
                case luisResultContainer.Intent.cancelDialog:
                    break;

                default:
                    var promptMessage = "I'm sorry, I am having trouble understanding you. What would you like me to do?";
                    return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
            }
            return await stepContext.ReplaceDialogAsync(InitialDialogId, "Could I help you with anything else?", cancellationToken);
        }

        private async Task displayProjects(projectBundle searchResult, WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //If we have less records than the search result limit, set the number of cards to make to what is available
            int numberOfSearchResults = searchResult.getNumberOfProjects() < SEARCH_RESULT_LIMIT
                ? searchResult.getNumberOfProjects() : SEARCH_RESULT_LIMIT;

            sendMessage(stepContext, "I found " + searchResult.getNumberOfProjects() + " related " + 
                                     (searchResult.getNumberOfProjects() == 1 ? "project. " : "projects. Here are the top results:") , cancellationToken);
            
            for (int i = searchIndex; i < searchIndex + numberOfSearchResults; i++)
            {
                Project currentRecord = searchResult.getProject(i);
                var response = MessageFactory.Attachment(currentRecord.getSimplePatientCard());
                await stepContext.Context.SendActivityAsync(response, cancellationToken);
            }

            //Assigns the current search to the scope of the dialog in case the user wants to do more with these results
            projectResults = searchResult;
        }

        private bool checkForEntities(luisResultContainer._Entities entities)
        {
            if (entities.contactJobTitle is null && entities.contactName is null && entities.organizationName is null
                && entities.projectUsages is null && entities.projectLocation is null &&
                entities.projectCriteria is null
                && entities.projectDescription is null && entities.organizationOverview is null) return false;
            return true;
        }
    } 
}
