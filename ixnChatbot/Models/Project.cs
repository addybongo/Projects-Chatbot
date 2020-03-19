using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ixnChatbot
{
    public class Project
    {
        protected readonly int ixnID;
        protected Dictionary<string, int> fields;
        protected string[] values;
        
        protected string fieldGetterQueryWholeTable
            = "SELECT COLUMN_NAME FROM information_schema.columns WHERE table_name in('IXN_database_entries')";
        protected string searchQuery;

        public Project(string[] fields, string[] values)
        {
            this.fields = new Dictionary<string, int>();
            this.values = values;

            for (int i = 0; i < fields.Length; i++)
            {
                this.fields.Add(fields[i], i);
            }

            ixnID = Int32.Parse(values[this.fields["ixnID"]]);
            searchQuery =
                "SELECT * FROM RCGP_Projects.IXN_database_entries i WHERE i.ixnID = " + ixnID;
        }

        //When a project is first instantiated, it only stores information on ixnID, ixnEntry, projectTitle,
        //organisationName and industrySupervisor. This method does a full SQL selection query and populates fields and
        //values with all the information available for this Project
        public void toDetailedProject()
        {
            sqlConnector _sqlConnector = new sqlConnector();
            _sqlConnector.OpenConnection();
            
            values = _sqlConnector.select(searchQuery)[0].ToArray();
            this.fields = new Dictionary<string, int>(); //Resets the field dictionary to empty
            
            // int ixnTableSize = _sqlConnector.select(fieldGetterQueryIXNTable).Count;
            string[] fields = transpose(_sqlConnector.select(fieldGetterQueryWholeTable)).ToArray();

            //Re add all values to the fields dictionary
            for (int i = 0; i < fields.Length; i++)
            {
                try {
                    this.fields.Add(fields[i], i);
                }
                catch (Exception) {
                    this.fields.Add("f." + fields[i], i);
                }
            }
        }

        public string getValue(string field)
        {
            int index;

            if (fields.TryGetValue(field, out index))
            {
                return values[index];
            }
            else
            {
                throw new Exception("The project with ID " + ixnID + " does not contain a value for the field "
                                    + field + "! Please check your field name or the database schema");
            }
        }
        
        private List<String> transpose(List<List<String>> data)
        {
            List<String> result = new List<String>();
            
            for (int i = 0; i < data.Count; i++)
            {
                result.Add(data[i][0]);
            }
            return result;
        }

        public bool hasIxnFormData()
        {
            return false;
        }
        
        public virtual Attachment getPatientCard()
        {
            string json = File.ReadAllText("Cards/detailedProjectCard.json");
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            jsonObj["body"][0]["columns"][1]["items"][0]["text"] = getValue("projectTitle");
            jsonObj["body"][0]["columns"][1]["items"][1]["text"] = getValue("organisationName");
            jsonObj["body"][1]["items"][0]["text"] = getValue("institute");
            jsonObj["body"][1]["items"][1]["text"] = "Led By " + getValue("industrySupervisor");
            jsonObj["body"][1]["items"][2]["text"] = "Uploaded On " + getValue("projectStart");
            jsonObj["body"][0]["columns"][0]["items"][0]["url"] = getOrganizationLogo(getValue("organisationName"));
            jsonObj["body"][3]["text"] = getValue("projectAbstract");

            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = jsonObj
            };
        }
        
        protected string getOrganizationLogo(string organizationName)
        {
            string json = File.ReadAllText("Cards/companyLogos.json");
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            JArray logos = (JArray) jsonObj["logos"];
            
            for (int i = 0; i < logos.Count; i++)
            {
                if (organizationName.ToLower().Contains(logos[i]["name"].ToString()))
                {
                    return logos[i]["link"].ToString();
                }
            }
            return jsonObj["default"];
        }
    }
}