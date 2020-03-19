using System;
using System.Collections.Generic;
using System.Linq;

namespace ixnChatbot
{
    public class projectBundle
    {
        private Project[] projects;
        private Dictionary<int, Project> idToProject;
        private readonly sqlConnector _sqlConnector;

        private string[] fields = {"ixnID", "ixnEntry", "projectTitle", "organisationName", "industrySupervisor"};
        
        public projectBundle(luisResultContainer._Entities entities)
        {
            _sqlConnector = new sqlConnector();
            _sqlConnector.OpenConnection();
            List<Project> projects = new List<Project>();

            string searchQuery = projectSelectionQueryBuilder(entities.contactJobTitle, entities.contactName,
                entities.organizationName,
                entities.projectUsages, entities.projectLocation, entities.projectCriteria, entities.projectDescription,
                entities.organizationOverview);

            List<List<String>> projectSearchResults = _sqlConnector.select(searchQuery);
            
            for (int i = 0; i < projectSearchResults.Count; i++)
            {
                if (projectSearchResults[i][getIndexOfField("ixnID", fields)] == "")
                {
                    projects.Add(new Project(fields, projectSearchResults[i].ToArray()));
                }
                else
                {
                    projects.Add(new IXN_Project(fields, projectSearchResults[i].ToArray()));
                }
            }
            this.projects = projects.ToArray();
            _sqlConnector.CloseConnection();
        }

        public Project getProject(int id)
        {
            return projects[id];
        }
        
        public Project getProjectByID(int id)
        {
            for (int i = 0; i < getNumberOfProjects(); i++)
            {
                if (projects[i].getValue("ixnID") == id.ToString())
                {
                    return projects[i];
                }
            }
            return null;
        }

        public int getNumberOfProjects()
        {
            return projects.Length;
        }

        private int getIndexOfField(string field, string[] fields)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i] == field) return i;
            }
            return -1;
        }
        
        private string projectSelectionQueryBuilder(String[] contactJobTitle, String[] contactName, String[] organizationName,
            String[] projectUsages,
            String[] projectLocation, String[] projectCriteria, String[] projectDescription, String[] organizationOverview)
        {
            string query = "SELECT i.ixnID, i.ixnEntry, i.projectTitle, i.organisationName, i.industrySupervisor " + 
                           "FROM RCGP_Projects.IXN_database_entries i LEFT JOIN RCGP_Projects.Projects p ON i.ixnEntry = p.projectID WHERE ";

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
                likeStatement += "p." + entityName + " LIKE '%" + entities[i] + "%' OR ";
            }
            likeStatement += "p." + entityName + " LIKE '%" + entities[entities.Length - 1] + "%'";
            
            return likeStatement;
        }
    }
}