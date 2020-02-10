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

        public sqlConnector()
        {
            server = "51.145.112.189";
            database = "RCGP_Projects";
            uid = "rcgpadmin";
            password = "rcgp!12345678";
            string connectionString = "SERVER=" + server + ";" + "DATABASE=" +
            database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";

            connection = new MySqlConnection(connectionString);
        }

        private bool OpenConnection()
        {
            try
            {
                connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("AN ERROR OCCURED IN SQL");
                return false;
            }
        }

        private bool CloseConnection()
        {
            try
            {
                connection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                return false;
            }
        }

        public List<string>[] selectOld(String query)
        {
            //Create a list to store the result
            List<string>[] list = new List<string>[2];
            list[0] = new List<string>();
            list[1] = new List<string>();

            //Open connection
            if (this.OpenConnection() == true)
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
                this.CloseConnection();

                //return list to be displayed
                return list;
            }
            else
            {
                return list;
            }
        }
        
        public List<List<String>> select(String query)
        {
            //Create a list to store the result
            List<List<String>> list = new List<List<String>>();

            //Open connection
            if (this.OpenConnection() == true)
            {
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

                //close Connection
                this.CloseConnection();

                //return list to be displayed
                return list;
            }
            else
            {
                return null;
            }
        }
        
        public string selectionQueryBuilder(String[] contactJob, String[] contactName, String[] organizationName,
            String[] projectDevice,
            String[] projectLocation, String[] projectSkill, String[] projectTitle)
        {
            string query = "SELECT projectTitle, organizationName, contactName FROM Projects WHERE ";

            if(contactJob != null) query+= likeStatementBuilder("contactJob", contactJob) + " OR ";
            if(contactName != null) query+= likeStatementBuilder("contactName", contactName) + " OR ";
            if(organizationName != null) query+= likeStatementBuilder("organizationName", organizationName) + " OR ";
            if(projectDevice != null) query+= likeStatementBuilder("projectDevice", projectDevice) + " OR ";
            if(projectLocation != null) query+= likeStatementBuilder("projectLocation", projectLocation) + " OR ";
            if(projectSkill != null) query+= likeStatementBuilder("projectSkill", projectSkill) + " OR ";
            if(projectTitle != null) query+= likeStatementBuilder("projectTitle", projectTitle) + " OR";

            return query.Substring(0, query.Length - 3) + ";";
        }
        
        private string likeStatementBuilder(String entityName, String[] entities)
        {
            string likeStatement = "";
            
            for (int i = 0; i < entities.Length - 1; i++)
            {
                likeStatement += entityName + " LIKE'%" + entities[i] + "%' OR ";
            }
            likeStatement += entityName + " LIKE'%" + entities[entities.Length - 1] + "%'";
            
            return likeStatement;
        }
    }
}
