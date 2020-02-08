using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using MySql.Data.MySqlClient;

namespace ixnChatbot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private static luisRecogniser _luisRecogniser;
        protected readonly BotState _UserState;
        static string name;

        public MainDialog()
            : base(nameof(MainDialog))
        {
            _luisRecogniser = _luisRecogniser;
            // Logger = _UserState;
            var waterfallSteps = new WaterfallStep[]
            {
                commandAsync,
                projectsAsync,
                confirmAsync,
                finalAsync
            };

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> commandAsync(WaterfallStepContext stepcontext, CancellationToken cancellationtoken)
        {
            // if (!_luisRecogniser.IsConfigured)
            // {
            //     await stepcontext.Context.SendActivityAsync(
            //         MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', " +
            //                             "'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationtoken);
            //
            //     return await stepcontext.NextAsync(null, cancellationtoken);
            // }
            
            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("Hi there! Please type in the organization you wish to search for.") };

            return await stepcontext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationtoken);
        }

        private static async Task<DialogTurnResult> projectsAsync(WaterfallStepContext stepcontext, CancellationToken cancellationtoken)
        {
            var response = (String)stepcontext.Result;
            sqlConnector connector = new sqlConnector();

            var searchingMessage = MessageFactory.Text("Ok, I am searching for projects matching your description...");
            await stepcontext.Context.SendActivityAsync(searchingMessage, cancellationtoken);

            // List<String>[] results = connector.Select("SELECT * FROM Projects WHERE organizationName='" + response + "';");
            List<String>[] results = connector.Select("SELECT * FROM Projects WHERE organizationName LIKE'%" + response + "%';");

            if(results[0].Count == 0)
            {
                var missingMessage = MessageFactory.Text("Sorry, I couldn't find any projects with this organization!");
                await stepcontext.Context.SendActivityAsync(missingMessage, cancellationtoken);
                return await stepcontext.EndDialogAsync(null, cancellationtoken);
            }

            var foundMessage = MessageFactory.Text("Found " + results[0].Count + " results:");
            await stepcontext.Context.SendActivityAsync(foundMessage, cancellationtoken);

            for (int i = 0; i < results[0].Count; i++)
            {
                var projectEntry = "#" + (i + 1) + ". Project Title: " + results[0][i] + " led by " + results[1][i];
                var projectEntryMessage = MessageFactory.Text(projectEntry, projectEntry);
                await stepcontext.Context.SendActivityAsync(projectEntryMessage, cancellationtoken);
            }
            return await stepcontext.ContinueDialogAsync(cancellationtoken);
        }
        private static async Task<DialogTurnResult> confirmAsync(WaterfallStepContext stepcontext, CancellationToken cancellationtoken)
        {

            var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("Did this answer your question?") };

            return await stepcontext.PromptAsync(nameof(ConfirmPrompt), promptOptions, cancellationtoken);

            return await stepcontext.EndDialogAsync(null, cancellationtoken);
        }

        private static async Task<DialogTurnResult> finalAsync(WaterfallStepContext stepcontext, CancellationToken cancellationtoken)
        {

            var response = (bool)stepcontext.Result;

            if(response)
            {
                var foundMessage = MessageFactory.Text("I'm glad I could help!");
                await stepcontext.Context.SendActivityAsync(foundMessage, cancellationtoken);
            }
            else
            {
                var foundMessage = MessageFactory.Text("I'm sorry I couldn't help! Try searching again with different keywords");
                await stepcontext.Context.SendActivityAsync(foundMessage, cancellationtoken);
            }
            return await stepcontext.EndDialogAsync(null, cancellationtoken);
        }
    }
}
