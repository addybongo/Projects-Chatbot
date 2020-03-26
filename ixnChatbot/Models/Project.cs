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
        protected readonly int projectID;
        private Dictionary<string, int> fields = new Dictionary<string, int>();
        private string[] values;
        private bool isAcademic;
        private bool hasNda;
        private bool hasContract;

        //Queries that gather fields from all 4 tables of schema
        protected string[] fieldGetterQueryWholeTable
            =
            {
                "SELECT COLUMN_NAME FROM information_schema.columns WHERE table_name in('projectentries')",
                "SELECT COLUMN_NAME FROM information_schema.columns WHERE table_name in('projects')",
                "SELECT COLUMN_NAME FROM information_schema.columns WHERE table_name in('contracts')",
                "SELECT COLUMN_NAME FROM information_schema.columns WHERE table_name in('academics')"
            };

        protected string searchQuery;

        public Project()
        {
            //This empty constructor is used to stop MS bot framework from trying to reconstruct the object when passing
            //between waterfall steps
        }

        public Project(string[] rawFields, string[] values)
        {
            this.values = values;
            isAcademic = values[3] == "" ? false : true; //values[3] stores the academicID; if it is missing, it isn't an academic project

            //Adds all fields and values to dictionary
            for (int i = 0; i < rawFields.Length; i++)
            {
                fields.Add(rawFields[i], i);
            }

            projectID = Int32.Parse(values[fields["projectID"]]);
            searchQuery =
                "SELECT * FROM RCGP_Projects.projectentries i LEFT JOIN RCGP_Projects.Projects p ON "
                + "i.ixnID = p.projectID LEFT JOIN RCGP_Projects.Contracts c ON i.contractID = c.contractID LEFT JOIN "
                + "RCGP_Projects.academics a ON i.academicID = a.academicID WHERE i.projectID = " + projectID;
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

            List<String> fields = new List<string>();  
            
            //Add all the fields into a single list
            for (int i = 0; i < fieldGetterQueryWholeTable.Length; i++)
            {
                fields.AddRange(transpose(_sqlConnector.select(fieldGetterQueryWholeTable[i])));
            }

            //Re add all values with their fields to the fields dictionary
            for (int i = 0; i < fields.Count; i++)
            {
                try
                {
                    this.fields.Add(fields[i], i);
                }
                catch (Exception)
                {
                    this.fields.Add("f." + fields[i], i); //If a key collision occurs (shouldn't be possible) append f. to it
                }
            }

            hasNda = (getValue("ndaRequired") == "True");
            isAcademic = getValue("academicID") != "";
            hasContract = getValue("requiresContract") == "True";

        }

        public string getValue(string field)
        {
            int index;

            if (fields.TryGetValue(field, out index))
            {
                return values[index];
            }

            throw new Exception("The project with ID " + projectID + " does not contain a value for the field "
                                    + field + "! Please check your field name or the database schema");
        }

        //Used to transpose when getting a list of fields, as they return as a 2d array of one column
        private List<String> transpose(List<List<String>> data)
        {
            List<String> result = new List<String>();

            for (int i = 0; i < data.Count; i++)
            {
                result.Add(data[i][0]);
            }

            return result;
        }

        public Attachment getPatientCard()
        {
            string json = File.ReadAllText("Cards/detailedIxnProjectCard.json");
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            
            //If the project is missing any one of the following attributes, the button to get to that card is removed
            if (!hasContract)
            {
                jsonObj["actions"][3] = "";
            }

            if (!hasNda)
            {
                jsonObj["actions"][4] = "";
            }

            if (!isAcademic)
            {
                jsonObj["actions"][5] = "";

            }

            jsonObj["body"][0]["columns"][1]["items"][0]["text"] = getValue("projectTitle");
            jsonObj["body"][0]["columns"][1]["items"][1]["text"] = getValue("organizationName");
            jsonObj["body"][1]["items"][0]["text"] = getValue("institute");
            jsonObj["body"][1]["items"][1]["text"] = "Led By " + getValue("contactName");
            jsonObj["body"][1]["items"][2]["text"] = "Uploaded On " + getValue("dateUploaded");

            jsonObj["body"][0]["columns"][0]["items"][0]["url"] = getOrganizationLogo(getValue("organizationName"));

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
            
            //Setup settings for back button so that it points back to the ID of this project
            jsonObj["body"][0]["columns"][0]["items"][0]["selectAction"]["data"]["data"] = "" + projectID;

            jsonObj["body"][2]["text"] = getValue("projectTitle");
            jsonObj["body"][4]["text"] = getValue("projectDescription");
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
            //Setup settings for back button so that it points back to the ID of this project
            jsonObj["body"][0]["columns"][0]["items"][0]["selectAction"]["data"]["data"] = "" + projectID;

            jsonObj["body"][0]["columns"][1]["items"][1]["text"] = getValue("projectTitle");
            
            //If any of the below values are empty, they are replaced with 'No <field> were specified'
            jsonObj["body"][2]["text"] = getValue("projectSkills") != ""
                ? getValue("projectSkills")
                : "No Skills were specified.";
            jsonObj["body"][4]["text"] = getValue("projectDataSamples") != ""
                ? getValue("projectDataSamples")
                : "No Data Samples were given.";
            jsonObj["body"][6]["text"] = getValue("projectDevices") != ""
                ? getValue("projectDevices")
                : "No Devices were specified.";

            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = jsonObj
            };
        }

        //Gets card of partner contact and organization
        public Attachment getPartnerCard()
        {
            string json = File.ReadAllText("Cards/industryPartnerCard.json");
            dynamic jsonObj = JsonConvert.DeserializeObject(json);
            jsonObj["body"][0]["columns"][0]["items"][0]["url"] = getOrganizationLogo(getValue("organizationName"));
            jsonObj["body"][0]["columns"][1]["items"][0]["text"] = getValue("organizationName");
            jsonObj["body"][2]["text"] = getValue("organizationOverview");
            jsonObj["body"][4]["text"] = getValue("organizationAddress");
            jsonObj["body"][6]["items"][0]["columns"][1]["items"][0]["text"] = getValue("contactName");
            jsonObj["body"][6]["items"][0]["columns"][1]["items"][1]["text"] = getValue("contactTitle");
            jsonObj["body"][6]["items"][0]["columns"][1]["items"][2]["text"] = getValue("contactNumber");
            jsonObj["body"][6]["items"][0]["columns"][1]["items"][3]["text"] = getValue("contactEmail");

            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = jsonObj
            };
        }

        //Simple card is returned when showing search results - it is smaller and more concise
        public Attachment getSimplePatientCard()
        {
            string json = File.ReadAllText("Cards/projectCard.json");
            dynamic jsonObj = JsonConvert.DeserializeObject(json);

            //Setup card so that when it is tapped, it points to this project ID
            jsonObj["selectAction"]["data"]["data"] = "" + projectID;

            jsonObj["body"][0]["text"] = getValue("projectTitle");
            jsonObj["body"][1]["columns"][1]["items"][0]["text"] = getValue("organizationName");
            jsonObj["body"][1]["columns"][1]["items"][1]["text"] = getValue("contactName");

            jsonObj["body"][1]["columns"][0]["items"][0]["url"] = getOrganizationLogo(getValue("organizationName"));

            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = jsonObj
            };
        }

        public Attachment getContractCard()
        {
            string json = File.ReadAllText("Cards/contractCard.json");
            dynamic jsonObj = JsonConvert.DeserializeObject(json);

            //Setup settings for back button so that it points back to the ID of this project
            jsonObj["body"][0]["columns"][0]["items"][0]["selectAction"]["data"]["data"] = "" + projectID;

            //All the points along the timeline of contract generation
            string[] timelinePoints = {"generatedContract", "studentSignedContract", "organizationSignedContract"};
            int timelinePointsCompleted = 0; //Used to keep track of whether all points were successfully created

            //Assigns a completed tick to the 'requires contract' timeline point as this must be true if a contract exists
            jsonObj["body"][2]["columns"][0]["items"][0]["columns"][0]["items"][0]["url"] =
                "https://upload.wikimedia.org/wikipedia/commons/thumb/0/0b/BlueFlat_tick_icon.svg/1200px-BlueFlat_tick_icon.svg.png";
            
            for (int i = 0; i < timelinePoints.Length; i++)
            {
                //If this timeline point has been completed, set it to appear so and add one to the counter of timelines completed
                if (getValue(timelinePoints[i]) == "True")
                {
                    jsonObj["body"][3 + i]["columns"][0]["items"][0]["columns"][0]["items"][0]["url"] =
                        "https://upload.wikimedia.org/wikipedia/commons/thumb/0/0b/BlueFlat_tick_icon.svg/1200px-BlueFlat_tick_icon.svg.png";
                    jsonObj["body"][3 + i]["columns"][0]["items"][0]["columns"][1]["items"][0]["text"] = "Completed";
                    jsonObj["body"][3 + i]["columns"][1]["items"][0]["url"] =
                        "https://webstockreview.net/images/circle-clipart-plain-18.png";
                    ++timelinePointsCompleted;
                }
            }

            //If all the timeline points were completed, the contract is completed 
            if (timelinePointsCompleted == timelinePoints.Length)
            {
                jsonObj["body"][6]["text"] = "Contract Completed on " + getValue("contractDateSigned");
            }

            //If the contract signatories section was filled out
            if (getValue("contractSignatories").Trim().Length != 0)
            {
                jsonObj["body"][7]["text"] = "Contract Signatories";
                jsonObj["body"][8]["text"] = getValue("contractSignatories");
            }

            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = jsonObj
            };
        }

        public Attachment getNDACard()
        {
            string json = File.ReadAllText("Cards/ndaCard.json");
            dynamic jsonObj = JsonConvert.DeserializeObject(json);

            //Setup settings for back button so that it points back to the ID of this project
            jsonObj["body"][0]["columns"][0]["items"][0]["selectAction"]["data"]["data"] = "" + projectID;

            //If the NDA was signed, set it to appear so
            if (getValue("ndaSigned") == "True")
            {
                jsonObj["body"][3]["columns"][0]["items"][0]["columns"][0]["items"][0]["url"] =
                    "https://upload.wikimedia.org/wikipedia/commons/thumb/0/0b/BlueFlat_tick_icon.svg/1200px-BlueFlat_tick_icon.svg.png";
                jsonObj["body"][3]["columns"][0]["items"][0]["columns"][1]["items"][0]["text"] = "Completed";
                jsonObj["body"][3]["columns"][1]["items"][0]["url"] =
                    "https://webstockreview.net/images/circle-clipart-plain-18.png";
                jsonObj["body"][4]["text"] = "NDA Completed on " + getValue("ndaDateSigned");

            }

            //If the contract signatories section was filled out
            if (getValue("ndaSignatories").Trim().Length != 0)
            {
                jsonObj["body"][5]["text"] = "NDA Signatories";
                jsonObj["body"][6]["text"] = getValue("ndaSignatories");
            }

            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = jsonObj
            };
        }

        public Attachment getAcademicCard()
        {
            string json = File.ReadAllText("Cards/academicCard.json");
            dynamic jsonObj = JsonConvert.DeserializeObject(json);

            //Setup settings for back button so that it points back to the ID of this project
            jsonObj["body"][0]["columns"][0]["items"][0]["selectAction"]["data"]["data"] = "" + projectID;
            
            //Create section that specifies institution
            if (getValue("institute").Trim().Length == 0)
            {
                jsonObj["body"][2]["columns"][1]["items"][0]["text"] = getValue("institute");
                jsonObj["body"][2]["columns"][0]["items"][0]["text"] = "Developed At";
            }

            //Add values regarding the reason for ethics disapproval if it occured
            if (getValue("ethicsApproval") == "False")
            {
                //Set ethics approval to 'No' and mark it in red. Also sets reason for ethics disapproval.
                jsonObj["body"][4]["columns"][1]["items"][0]["text"] = "No";
                jsonObj["body"][4]["columns"][1]["items"][0]["color"] = "Attention";
                jsonObj["body"][4]["columns"][0]["items"][1]["text"] = "Reason for Ethics Disapproval";
                jsonObj["body"][4]["columns"][1]["items"][1]["text"] = getValue("reasonForEthicsDisapproval");
            }

            //If the ethics assessor info is available, add it
            if (getValue("ethicsAssessor").Trim().Length != 0)
            {
                jsonObj["body"][4]["columns"][0]["items"][2]["text"] = "Ethics Assessor";
                jsonObj["body"][4]["columns"][1]["items"][2]["text"] = getValue("ethicsAssessor");
            }

            //If any of the below sections are not available, they are replaced with "N/A" or To be assessed for date
            jsonObj["body"][6]["columns"][1]["items"][0]["text"] = getValue("primaryAssessor").Trim().Length == 0
                ? "N/A"
                : getValue("primaryAssessor");
            jsonObj["body"][6]["columns"][1]["items"][1]["text"] = getValue("secondaryAssessor").Trim().Length == 0
                ? "N/A"
                : getValue("secondaryAssessor");
            jsonObj["body"][6]["columns"][1]["items"][2]["text"] = getValue("dateAssessed").Trim().Length == 0
                ? "To be Assessed"
                : getValue("dateAssessed");
            ;

            //If comments were added, add them here
            if (getValue("comments").Trim().Length == 0)
            {
                jsonObj["body"][7]["text"] = "";
            }
            else jsonObj["body"][8]["text"] = getValue("comments");

            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = jsonObj
            };
        }

        //This method gets the corresponding link for each company logo based on the organization name
        //If it isn't found, a default image of gears is used
        private string getOrganizationLogo(string organizationName)
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

        public bool HasNda()
        {
            return hasNda;
        }

        public bool IsAcademic()
        {
            return isAcademic; 
        }
        
        public bool HasContract()
        {
            return hasContract; 
        }
    }
}