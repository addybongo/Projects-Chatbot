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
            getProjectByContact,
            getProjectByDetails,
            getProjectByOrganization,
            listProjects,
            None
        };
        
        public Dictionary<Intent, IntentScore> intents;
        public IDictionary<string, object> properties { get; set; }
        
        public class Entities
        {
            public string contactEmail;
            public string contactJobTitle;
            public string contactName;
            public string contactNumber;
            public string organizationAddress;
            public string organizationName;
            public string overviewOrganization;
            public string projectDevices;
            public string projectLocation;
            public string projectSkills;
            public string projectTitle;
            public string projectTechnicalChallenges;
            public string projectDataSamples;
            public string projectDescription;
            public string projectRequirements;
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