using System;
using System.Collections.Generic;

namespace ixnChatbot
{
    public class Project
    {
        protected readonly int ixnID;
        protected Dictionary<string, int> fields;
        protected string[] values;
        private bool hasIXNFormData;
        
        private static readonly string fieldGetterQueryWholeTable
            = "SELECT COLUMN_NAME FROM information_schema.columns WHERE table_name in('Projects','IXN_database_entries')";

        public Project(string[] fields, string[] values, bool hasIxnFormData)
        {
            this.fields = new Dictionary<string, int>();
            this.values = values;
            this.hasIXNFormData = hasIxnFormData;

            for (int i = 0; i < fields.Length; i++)
            {
                //There may be fields with the same name e.g. Project Title in both tables. In this case, the try block
                //is used so that the constructor doesn't crash. If the class needs to store this second instance of the
                //field e.g. if it has an IXN form, it handles all that in the inheriting class, IXN_Project
                try
                {
                    this.fields.Add(fields[i], i);
                }
                catch (Exception)
                {
                    this.fields.Add("f." + fields[i], i);
                }
            }

            ixnID = Int32.Parse(values[this.fields["ixnID"]]);
        }

        //When a project is first instantiated, it only stores information on ixnID, ixnEntry, projectTitle,
        //organisationName and industrySupervisor. This method does a full SQL selection query and populates fields and
        //values with all the information available for this Project
        public void toDetailedProject()
        {
            string searchQuery =
                "SELECT * FROM RCGP_Projects.IXN_database_entries i LEFT JOIN RCGP_Projects.Projects p ON "
                + "i.ixnEntry = p.projectID WHERE i.ixnID = " + ixnID;
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
            return hasIXNFormData;
        }
    }
}