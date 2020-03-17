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
    public class searchProject2 : dialogBase
    {
        private int SEARCH_RESULT_LIMIT = 4;
        private Project project; //The project that this dialog focuses on
        
        public searchProject2(luisRecogniser luisRecogniser) : base(luisRecogniser, nameof(searchProject2))
        {
            jsonManager = new jsonManager();

            var waterfallSteps = new WaterfallStep[]
            {
                partOne,
                cardOrUser,
                partTwo
            };

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> partOne(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            project = (Project) stepContext.Options;
            project.toDetailedProject();
            Attachment projectCard =  jsonManager.detailedProjectCardGenerator(project.getValue("projectTitle"), 
                project.getValue("organisationName"), project.getValue("institute"),
                project.getValue("industrySupervisor"), project.getValue("projectStart"));
            var response = MessageFactory.Attachment(projectCard);
            
            await stepContext.Context.SendActivityAsync(response, cancellationToken);

            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("What would you like to know about this project?") };
            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
        }
        
        private async Task<DialogTurnResult> cardOrUser(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            //Message Code sent if user clicks on a card
            if (stepContext.Result.ToString() == "#AC_SP")
            {
                return await stepContext.ReplaceDialogAsync(nameof(searchProject), stepContext.Context.Activity.Value, cancellationToken);
            }
            //Proceed to LUIS if the message was typed by user...
            return await stepContext.NextAsync(stepContext.Result, cancellationToken);
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
    } 
}
