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

        private string[] fields = {"projectID", "ixnID", "contractID", "academicID", "projectTitle", "organizationName", "contactName"};
        
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
            
            //Create a Project object for each project found and populate it with its values
            for (int i = 0; i < projectSearchResults.Count; i++)
            {
                projects.Add(new Project(fields, projectSearchResults[i].ToArray()));
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
                if (projects[i].getValue("projectID") == id.ToString())
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

        private string projectSelectionQueryBuilder(String[] contactJobTitle, String[] contactName, String[] organizationName,
            String[] projectUsages,
            String[] projectLocation, String[] projectCriteria, String[] projectDescription, String[] organizationOverview)
        {
            string query = "SELECT i.projectID, i.ixnID, i.contractID, i.academicID, p.projectTitle, "
            + "p.organizationName, p.contactName FROM RCGP_Projects.projectentries i LEFT JOIN RCGP_Projects.Projects p ON i.ixnID = p.projectID WHERE ";

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
            if(projectLocation != null) query+= likeStatementBuilder("organizationAddress", projectLocation) + " OR ";
            if (projectUsages != null)
            {
                query+= likeStatementBuilder("projectDevices", projectUsages) + " OR " + 
                        likeStatementBuilder("projectDataSamples", projectUsages) + " OR " + 
                        likeStatementBuilder("anyOtherInformation", projectUsages) + " OR ";
            }
            //If LUIS identified any entities, the if statements above would be executed and at the end of the query,
            //'OR ' would be left. This lets us isolate the case where no entities are found, so that we get rid of
            //the WHERE clause in the query
            if (query.Substring(query.Length-3, 3) == "OR ")
            {
                return query.Substring(0, query.Length - 3) + ";";
            }
            //Gets rid of the WHERE clause
            return query.Substring(0, query.Length - 6);
        }
        
        private string likeStatementBuilder(String field, String[] entities)
        {
            string likeStatement = "";
            
            for (int i = 0; i < entities.Length - 1; i++)
            {
                likeStatement += "p." + field + " LIKE '%" + entities[i] + "%' OR ";
            }
            likeStatement += "p." + field + " LIKE '%" + entities[entities.Length - 1] + "%'";
            
            return likeStatement;
        }
    }
}