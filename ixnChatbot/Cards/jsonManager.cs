using System.IO;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ixnChatbot.Cards
{
    public class jsonManager
    {
        //Constructs a chatbot card from a JSON file (see Cards Folder)
        public Attachment CreateAdaptiveCardAttachment()
        {
            var cardResourcePath = "ixnChatbot.Cards.welcomeCard.json";

            using (var stream = GetType().Assembly.GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var adaptiveCard = reader.ReadToEnd();
                    return new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    };
                }
            }
        }

        public Attachment projectCardGenerator(string id, string projectTitle, string organizationName, string contactName)
        {
            string json = File.ReadAllText("Cards/projectCard.json");
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            jsonObj["selectAction"]["data"]["id"] = "#AC_SP";
            jsonObj["selectAction"]["data"]["data"] = id;
            jsonObj["body"][0]["text"] = projectTitle;
            jsonObj["body"][1]["columns"][1]["items"][0]["text"] = organizationName;
            jsonObj["body"][1]["columns"][1]["items"][1]["text"] = contactName;

            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = jsonObj
            };
        }
        
        public Attachment detailedProjectCardGenerator(string projectTitle, string organizationName, string institution, string contactName, string date)
        {
            string json = File.ReadAllText("Cards/projectDetailedCard.json");
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            jsonObj["body"][0]["columns"][1]["items"][0]["text"] = projectTitle;
            jsonObj["body"][0]["columns"][1]["items"][1]["text"] = organizationName;
            jsonObj["body"][1]["items"][0]["text"] = institution;
            jsonObj["body"][1]["items"][1]["text"] = "Led By " + contactName;
            jsonObj["body"][1]["items"][2]["text"] = "Uploaded On " + date;

            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = jsonObj
            };
        }
    }
}