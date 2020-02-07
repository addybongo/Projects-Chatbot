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
            
        }
    }
}
