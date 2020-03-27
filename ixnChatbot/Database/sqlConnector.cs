﻿using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
 using System.IO;
 using System.Linq;
using System.Threading.Tasks;
 using Newtonsoft.Json;

 namespace ixnChatbot
{
    public class sqlConnector
    {
        private MySqlConnection connection;
        private bool connected;

        public sqlConnector()
        {
            string configFile = File.ReadAllText("Database/dbconfig.json");
            dynamic config = JsonConvert.DeserializeObject(configFile);

            string connectionString = "SERVER=" + config["server"] + ";" + "DATABASE=" +
                                      config["database"] + ";" + "UID=" + config["username"] + ";" + "PASSWORD=" + 
                                      config["password"] + ";";

            connection = new MySqlConnection(connectionString);
        }

        public void OpenConnection()
        {
            try
            {
                connection.Open();
                connected = true;
            }
            catch (MySqlException e)
            {
                throw new Exception("There is an error in the MySQL settings provided in dbconfig.json. Please check and try again.");
            }
        }

        public void CloseConnection()
        {
            try
            {
                connection.Close();
                connected = false;
            }
            catch (MySqlException)
            {
                throw new Exception("The connection could not be closed! Please check the database settings and" +
                                    "restart the chatbot server.");
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
    }
}
