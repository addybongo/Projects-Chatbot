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

        public IXN_Project()
        {
            //This empty constructor is used to stop MS bot framework from trying to reconstruct the object when passing
            //between waterfall steps
        }
        
        public IXN_Project(string[] rawFields, string[] values)
        :base(rawFields, values)
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
            jsonObj["body"][0]["columns"][0]["items"][0]["selectAction"]["data"]["data"] = "" + ixnID;
            jsonObj["body"][2]["text"] = getValue("projectTitle");
            jsonObj["body"][4]["text"] = getValue("projectAbstract");
            jsonObj["body"][6]["text"] = getValue("projectTechnicalChallenges");
            
            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = jsonObj
            };
        }
        
        public Attachment getSkillsDataAndDevicesCard()
        {
            string json = File.ReadAllText("Cards/skillsDataAndDevicesCard.json");
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            jsonObj["body"][0]["columns"][0]["items"][0]["selectAction"]["data"]["data"] = "" + ixnID;
            jsonObj["body"][0]["columns"][1]["items"][1]["text"] = getValue("projectTitle");
            jsonObj["body"][2]["text"] = getValue("projectSkills");
            jsonObj["body"][4]["text"] = getValue("projectDataSamples");
            jsonObj["body"][6]["text"] = getValue("projectDevices"); 
            
            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = jsonObj
            };
        }
        
        public Attachment getPartnerCard()
        {
            string json = File.ReadAllText("Cards/industryPartnerCard.json");
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            jsonObj["body"][0]["columns"][1]["items"][0]["text"] = getValue("organizationName");
            jsonObj["body"][2]["text"] = getValue("organizationOverview");
            jsonObj["body"][4]["text"] = getValue("organizationAddress");
            jsonObj["body"][6]["items"][0]["columns"][1]["items"][0]["text"] = getValue("contactName"); 
            jsonObj["body"][6]["items"][0]["columns"][1]["items"][1]["text"] = getValue("contactTitle"); 
            jsonObj["body"][6]["items"][0]["columns"][1]["items"][1]["text"] = getValue("contactTitle");
            jsonObj["body"][6]["items"][0]["columns"][1]["items"][2]["text"] = getValue("contactNumber");
            jsonObj["body"][6]["items"][0]["columns"][1]["items"][2]["text"] = getValue("contactEmail");


            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = jsonObj
            };
        }
    }
}