using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace ixnChatbot.Dialogs
{
    public class searchProject : dialogBase
    {
        private int SEARCH_RESULT_LIMIT = 4;
        private Project project; //The project that this dialog focuses on
        
        public searchProject(luisRecogniser luisRecogniser) : base(luisRecogniser, nameof(searchProject))
        {
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
            //If options given to dialog, it has just begun, which means we show the starting menu card
            if (stepContext.Options != null)
            {
                project ??= (Project) stepContext
                    .Options; 
                project.toDetailedProject();
                await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(project.getPatientCard()),
                    cancellationToken);
            }

            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("What would you like to know about this project?") };
            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
        }
        
        private async Task<DialogTurnResult> cardOrUser(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            IXN_Project asIxnProject = (IXN_Project) project;

            switch (stepContext.Result.ToString())
            { 
                case "#AC_SP":
                    return await stepContext.ReplaceDialogAsync(InitialDialogId, project, cancellationToken);
                case "#AC_Description":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(asIxnProject.getDescriptionCard()),
                        cancellationToken);
                    return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
                    break;
                case "#AC_SDD":
                    asIxnProject = (IXN_Project) project;
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(asIxnProject.getSkillsDataAndDevicesCard()),
                        cancellationToken);
                    return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
                case "#AC_Partner":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(asIxnProject.getPartnerCard()),
                        cancellationToken);
                    return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
            }
            //Proceed to LUIS if the message was typed by user...
            return await stepContext.NextAsync(stepContext.Result.ToString(), cancellationToken);
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
                    return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
            }
        }

        private string resolveCardActionCode(string activityValue)
        {
            dynamic jsonObj = JsonConvert.DeserializeObject(activityValue);
            return jsonObj["id"];
        }
    } 
}
