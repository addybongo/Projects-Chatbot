﻿using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ixnChatbot
{
    public class sqlConnector
    {
        private MySqlConnection connection;
        private string server;
        private string database;
        private string uid;
        private string password;

        private bool connected;

        public sqlConnector()
        {
        server = "rcgpprojects.mysql.database.azure.com";
        database = "RCGP_Projects";
        uid = "rcgpadmin@rcgpprojects";
        password = "rcgp!12345678";
        string connectionString = "SERVER=" + server + ";" + "DATABASE=" +
        database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";
            
            // server = "51.145.112.189";
            // database = "RCGP_Projects";
            // uid = "rcgpadmin";
            // password = "rcgp!12345678";
            // string connectionString = "SERVER=" + server + ";" + "DATABASE=" +
            // database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";

            connection = new MySqlConnection(connectionString);
        }

        public bool OpenConnection()
        {
            try
            {
                connection.Open();
                connected = true;
                return true;
            }
            catch (MySqlException)
            {
                throw new Exception("The connection setting for the projects database are incorrect. Please " +
                                    "change the configuration.");
            }
        }

        public bool CloseConnection()
        {
            try
            {
                connection.Close();
                connected = false;
                return true;
            }
            catch (MySqlException ex)
            {
                throw new Exception("The connection could not be closed! Please check the database settings and" +
                                    "restart the chatbot server.");
            }
        }

        public List<string>[] selectOld(String query)
        {
            //Create a list to store the result
            List<string>[] list = new List<string>[2];
            list[0] = new List<string>();
            list[1] = new List<string>();

            //Open connection
            if (OpenConnection())
            {
                //Create Command
                MySqlCommand cmd = new MySqlCommand(query, connection);
                //Create a data reader and Execute the command
                MySqlDataReader dataReader = cmd.ExecuteReader();

                //Read the data and store them in the list
                while (dataReader.Read())
                {
                    list[0].Add(dataReader["projectTitle"] + "");
                    list[1].Add(dataReader["contactName"] + "");
                }

                //close Data Reader
                dataReader.Close();

                //close Connection
                CloseConnection();

                //return list to be displayed
                return list;
            }
            else
            {
                return list;
            }
        }
        
        public List<List<String>> select(string query)
        {
            //Create a list to store the result
            List<List<String>> list = new List<List<String>>();

            //Check if connection hasn't been opened
            if (!connected)
            {
                return null;
            }
            //Create Command
            MySqlCommand cmd = new MySqlCommand(query, connection);
            //Create a data reader and Execute the command
            MySqlDataReader dataReader = cmd.ExecuteReader();

            //Read the data and store them in the list
            while (dataReader.Read())
            {
                List<String> record = new List<string>();
            
                for (int i = 0; i < dataReader.FieldCount; i++)
                {
                    record.Add("" + dataReader[i]);
                }
                list.Add(record);
            }
            //close Data Reader
            dataReader.Close();
            
            //return list to be displayed
            return list;
        }

        public List<String> getFieldNames(string tableName)
        {
            List<String> fields = new List<string>();
            string query = "DESCRIBE " + tableName + ";";
            List<List<String>> queryResult = select(query);
            
            for (int i = 0; i < queryResult.Count; i++)
            {
                fields.Add(queryResult[i][0]);
            }
            return fields;
        }

        public string projectSelectionQueryBuilder(String[] contactJobTitle, String[] contactName, String[] organizationName,
            String[] projectUsages,
            String[] projectLocation, String[] projectCriteria, String[] projectDescription, String[] organizationOverview)
        {
            string query = "SELECT projectID, projectTitle, organizationName, contactName FROM Projects WHERE ";

            if(contactJobTitle != null) query+= likeStatementBuilder("contactTitle", contactJobTitle) + " OR ";
            if(contactName != null)
            {
                query+= likeStatementBuilder("contactName", contactName) + " OR " + 
                        likeStatementBuilder("contactEmail", contactName) + " OR ";
            }
            if(organizationName != null) query+= likeStatementBuilder("organizationName", organizationName) + " OR ";
            if(organizationOverview != null) query+= likeStatementBuilder("organizationOverview", organizationOverview) + " OR ";
            if(projectCriteria != null)
            {
                query += likeStatementBuilder("projectRequirements", projectCriteria) + " OR " +
                         likeStatementBuilder("projectTechnicalChallenges", projectCriteria) + " OR " +
                         likeStatementBuilder("projectSkills", projectCriteria) + " OR " +
                         likeStatementBuilder("anyOtherInformation", projectCriteria) + " OR ";
            }
            if (projectDescription != null)
            {
                query+= likeStatementBuilder("projectTitle", projectDescription) + " OR " + 
                        likeStatementBuilder("projectDescription", projectDescription) + " OR " + 
                        likeStatementBuilder("anyOtherInformation", projectDescription) + " OR ";
            }
            if(projectLocation != null) query+= likeStatementBuilder("projectLocation", projectLocation) + " OR ";
            if (projectUsages != null)
            {
                query+= likeStatementBuilder("projectDevices", projectUsages) + " OR " + 
                        likeStatementBuilder("projectDataSamples", projectUsages) + " OR " + 
                        likeStatementBuilder("anyOtherInformation", projectUsages) + " OR ";
            }
            return query.Substring(0, query.Length - 3) + ";";
        }
        
        private string likeStatementBuilder(String entityName, String[] entities)
        {
            string likeStatement = "";
            
            for (int i = 0; i < entities.Length - 1; i++)
            {
                likeStatement += entityName + " LIKE '%" + entities[i] + "%' OR ";
            }
            likeStatement += entityName + " LIKE '%" + entities[entities.Length - 1] + "%'";
            
            return likeStatement;
        }
    }
}
