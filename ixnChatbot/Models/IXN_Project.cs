using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace ixnChatbot
{
    public class IXN_Project : Project
    {
        private readonly Dictionary<string, int> moreFields;

        public IXN_Project(string[] fields, string[] values)
        :base(fields, values)
        {
            fieldGetterQueryWholeTable
                = "SELECT COLUMN_NAME FROM information_schema.columns WHERE table_name in('Projects','IXN_database_entries')";
            searchQuery =
                "SELECT * FROM RCGP_Projects.IXN_database_entries i LEFT JOIN RCGP_Projects.Projects p ON "
                + "i.ixnEntry = p.projectID WHERE i.ixnID = " + ixnID;
        }
        
        public bool hasIxnFormData()
        {
            return true;
        }

        public override Attachment getPatientCard()
        {
            string json = File.ReadAllText("Cards/detailedIxnProjectCard.json");
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            jsonObj["body"][0]["columns"][1]["items"][0]["text"] = getValue("projectTitle");
            jsonObj["body"][0]["columns"][1]["items"][1]["text"] = getValue("organisationName");
            jsonObj["body"][1]["items"][0]["text"] = getValue("institute");
            jsonObj["body"][1]["items"][1]["text"] = "Led By " + getValue("contactName");
            jsonObj["body"][1]["items"][2]["text"] = "Uploaded On " + getValue("projectStart");
            
            jsonObj["body"][0]["columns"][0]["items"][0]["url"] = getOrganizationLogo(getValue("organisationName"));

            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = jsonObj
            };
        }
        
        public Attachment getDescriptionCard()
        {
            string json = File.ReadAllText("Cards/descriptionCard.json");
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            jsonObj["body"][2]["text"] = getValue("projectTitle");
            jsonObj["body"][4]["text"] = "Uploaded On " + getValue("projectAbstract");
            jsonObj["body"][6]["text"] = "Uploaded On " + getValue("f.projectTechnicalChallenges");

            jsonObj["body"][0]["columns"][0]["items"][0]["url"] = getOrganizationLogo(getValue("organisationName"));

            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = jsonObj
            };
        }
    }
}