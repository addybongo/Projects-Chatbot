using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
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
            string welcomeMessage = "What would you like to know about this project?";

            if (stepContext.Options != null)
            {
                if (stepContext.Options.GetType() == typeof(Project))
                {
                    project ??= (Project) stepContext.Options;
                    project.toDetailedProject();
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(project.getPatientCard()),
                        cancellationToken);
                }
                else welcomeMessage = stepContext.Options.ToString();
            }

            var promptOptions = new PromptOptions  { Prompt = MessageFactory.Text(welcomeMessage) };
            
            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
        }
        
        private async Task<DialogTurnResult> cardOrUser(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {

            switch (stepContext.Result.ToString())
            { 
                case "#AC_SP":
                    return await stepContext.ReplaceDialogAsync(InitialDialogId, project, cancellationToken);
                case "#AC_Description":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(project.getDescriptionCard()),
                        cancellationToken);
                    break;
                case "#AC_SDD":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(project.getSkillsDataAndDevicesCard()),
                        cancellationToken);
                    break;
                case "#AC_Partner":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(project.getPartnerCard()),
                        cancellationToken);
                    break;
                case "#AC_Contract":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(project.getContractCard()),
                        cancellationToken);
                    break;
                case "#AC_NDA":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(project.getNDACard()),
                        cancellationToken);
                    break;
                case "#AC_Academic":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(project.getAcademicCard()),
                        cancellationToken);
                    break;
                default:
                    //Proceed to LUIS if the message was typed by user...
                    return await stepContext.NextAsync(stepContext.Result.ToString(), cancellationToken);
            }

            string continueMessage = "What else would you like to know?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, continueMessage, cancellationToken);
        }

        private async Task<DialogTurnResult> partTwo(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var luisResult = await _luisRecogniser.RecognizeAsync<luisResultContainer>(stepContext.Context, cancellationToken);

            switch (luisResult.TopIntent().intent)
            {
                case luisResultContainer.Intent.displayMoreProjects:
                case luisResultContainer.Intent.listProjects:
                    return await stepContext.EndDialogAsync(stepContext.Result, cancellationToken);

                case luisResultContainer.Intent.descriptionOfProject:
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(project.getDescriptionCard()),
                        cancellationToken);
                    break;

                case luisResultContainer.Intent.contractOfProject:
                    if(!project.HasContract()) sendMessage(stepContext, "This project doesn't have any contractual agreement", cancellationToken);
                    else await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(project.getContractCard()),
                        cancellationToken);
                    break;

                case luisResultContainer.Intent.ndaOfProject:
                    if (!project.HasNda()) sendMessage(stepContext, "This project doesn't have an NDA", cancellationToken);
                    else await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(project.getNDACard()),
                        cancellationToken);
                    break;

                case luisResultContainer.Intent.partnerOfProject:
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(project.getPartnerCard()),
                        cancellationToken);
                    break;

                case luisResultContainer.Intent.academicOfProject:
                    if(!project.IsAcademic()) sendMessage(stepContext, "That information is only stored with academic projects, not this one.", cancellationToken);
                    else await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(project.getAcademicCard()),
                        cancellationToken);
                    break;
                
                case luisResultContainer.Intent.cancelDialog:
                    sendMessage(stepContext, "Glad I could help!", cancellationToken);
                    return await stepContext.EndDialogAsync(stepContext.Result, cancellationToken);

                default:
                    string errorMessage = "I'm sorry, I don't understand that. What would you like to know?";
                    return await stepContext.ReplaceDialogAsync(InitialDialogId, errorMessage, cancellationToken);
            }

            string continueMessage = "Is there anything else you'd like to know?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, continueMessage, cancellationToken);
        }
    } 
}
