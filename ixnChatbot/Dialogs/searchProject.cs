using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ixnChatbot.Cards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

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

            AddDialog(new WaterfallDialog("waterfall1", waterfallSteps));
            AddDialog(new TextPrompt("intentPrompt2"));

            InitialDialogId = "waterfall1";
        }

        private async Task<DialogTurnResult> partOne(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            //await stepContext.Context.SendActivityAsync(MessageFactory.Text("NOOT NOOT " + stepContext.Result), cancellationToken);

            //return await stepContext.EndDialogAsync("show me projects led by joseph connor", cancellationToken);
            
            var messageText = "What would you like to know about this project?";
            
            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text(messageText) };
            return await stepContext.PromptAsync("intentPrompt2", promptOptions, cancellationToken);
        }
        
        private async Task<DialogTurnResult> partTwo(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            sendMessage(stepContext, "yeet", cancellationToken);
            
            var luisResult = await _luisRecogniser.RecognizeAsync<luisResultContainer>(stepContext.Context, cancellationToken);
            luisResultContainer._Entities entities = luisResult.Entities;

            switch (luisResult.TopIntent().intent)
            {
                case luisResultContainer.Intent.listProjects:
                    return await stepContext.EndDialogAsync("show me projects led by joseph connor", cancellationToken);
                default:
                    sendMessage(stepContext, "Result: " +  stepContext.Result, cancellationToken);
                    return await stepContext.ReplaceDialogAsync("waterfall1", null, cancellationToken);
            }
        }
    } 
}
