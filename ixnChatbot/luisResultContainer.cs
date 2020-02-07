using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Newtonsoft.Json;

namespace ixnChatbot
{
    //The purpose of this class is to act as a container for the result of a query to LUIS. It captures all the data
    //returned when using the LUIS API
    
    public class luisResultContainer : IRecognizerConvert
    {
        public string text;
        public string alteredText;

        public enum Intent
        {
            listProjects,
            None
        };
        
        public Dictionary<Intent, IntentScore> intents;
        public IDictionary<string, object> properties { get; set; }
        
        public class Entities
        {
            public string contactJob;
            public string contactName;
            public string organizationName;
            public string projectDevice;
            public string projectLocation;
            public string projectSkill;
            public string projectTitle;
        }
        
        public Entities entities;
        
        
        public void Convert(dynamic result)
        {
            var app = JsonConvert.DeserializeObject<luisResultContainer>(JsonConvert.SerializeObject(result, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            text = app.Text;
            alteredText = app.AlteredText;
            intents = app.Intents;
            entities = app.Entities;
            properties = app.Properties;
        }
    }
}