using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace ixnChatbot.Dialogs
{
    public class luisDialogTest : ComponentDialog
    {
        private readonly luisRecogniser _luisRecogniser;
        
        public luisDialogTest(luisRecogniser luisRecogniser)
        {
            _luisRecogniser = luisRecogniser;
            
            var waterfallSteps = new WaterfallStep[]
            {
                intentPrompt,
                intentAnswer
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
                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text("List Projects"), cancellationToken);
                    break;
                case luisResultContainer.Intent.None:
                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text("No Intent"), cancellationToken);
                    break;
            }

            var promptMessage = "Please type in another sentence for me to examine";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    } 
}
