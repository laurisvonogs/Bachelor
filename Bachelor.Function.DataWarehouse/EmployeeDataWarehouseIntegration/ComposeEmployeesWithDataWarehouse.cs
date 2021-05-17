using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using EmployeeDataWarehouseIntegration.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;

namespace EmployeeDataWarehouseIntegration
{
    public static class ComposeEmployeesWithDataWarehouse
    {
        [FunctionName("ComposeEmployeesWithDataWarehouse")]
        public static void Run([TimerTrigger("3 0 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var rawEmployees = GetRawEmployees();
            log.LogInformation("Retrieved Employees from input system");

            var composedEmployees = ComposeRawEmployees(rawEmployees);
            log.LogInformation("Composed Employees");

            PostEmployeesToDataWarehouse(composedEmployees, log);
            log.LogInformation("Sent Employees to data warehouse");
        }
        private static List<RawEmployee> GetRawEmployees()
        {
            var request = new RestRequest(Method.GET);
            request.AddHeader("X-API-Key", Environment.GetEnvironmentVariable("INPUT_API_KEY"));
            var inputApiUrl = Environment.GetEnvironmentVariable("INPUT_API_URL");
            var getEmployeesURL = new RestClient($"{inputApiUrl}/users.json");
            IRestResponse employeesResponse = getEmployeesURL.Execute(request);

            var employeeList = JsonConvert.DeserializeObject<List<RawEmployee>>(employeesResponse.Content);
            return employeeList;
        }
        private static List<ComposedEmployee> ComposeRawEmployees(List<RawEmployee> rawEmployees)
        {
            var composedEmpList = new List<ComposedEmployee>();
            foreach (var emp in rawEmployees)
            {
                var composedEmp = new ComposedEmployee
                {
                    Address = $"{emp.Street_name} {emp.Street_number}, {emp.Postal_code} {emp.City}, {emp.Country}",
                    Company = emp.Company_name,
                    Full_Name = $"{emp.First_name} {emp.Last_name}",
                    Gender = emp.Gender == "Male" ? "M" : "F",
                    ID = emp.Person_id,
                    isActive = emp.Contract_until >= DateTime.UtcNow,
                    Job_title = emp.Job_title,
                    Mail = emp.Email,
                    Works_from = emp.Works_from_office == true ? emp.Main_office : "Home",
                    Integration_type = "Function"
                };
                composedEmpList.Add(composedEmp);
            }
            return composedEmpList;
        }
        private static void PostEmployeesToDataWarehouse(List<ComposedEmployee> composedEmployees, ILogger log)
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = Environment.GetEnvironmentVariable("SQLSERVER-DOMAIN-NAME");
                builder.UserID = Environment.GetEnvironmentVariable("DATAWAREHOUSE-USERNAME");
                builder.Password = Environment.GetEnvironmentVariable("DATAWAREHOUSE-PASSWORD");
                builder.InitialCatalog = Environment.GetEnvironmentVariable("DATAWAREHOUSE-NAME");

                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    foreach (var emp in composedEmployees)
                    {
                        string query = $@"
                        IF EXISTS(SELECT * FROM [dbo].[Emp_fun] WHERE ID = {emp.ID}) 
                            UPDATE [dbo].[Emp_fun] SET 
                                Full_Name = '{emp.Full_Name.Replace("'", "''")}', 
                                Address = '{emp.Address.Replace("'", "''")}', 
                                Company = '{emp.Company.Replace("'", "''")}', 
                                Gender = '{emp.Gender.Replace("'", "''")}', 
                                isActive = '{emp.isActive}', 
                                Job_title = '{emp.Job_title.Replace("'", "''")}', 
                                Mail = '{emp.Mail.Replace("'", "''")}', 
                                Works_from = '{emp.Works_from.Replace("'", "''")}', 
                                Integration_type = '{emp.Integration_type.Replace("'", "''")}' 
                            WHERE ID = {emp.ID} 
                        ELSE 
                            INSERT INTO [dbo].[Emp_fun] VALUES(
                                '{emp.ID}', 
                                '{emp.Full_Name.Replace("'", "''")}',
                                '{emp.Address.Replace("'", "''")}',
                                '{emp.Company.Replace("'", "''")}',
                                '{emp.Gender.Replace("'", "''")}',
                                '{emp.isActive}',
                                '{emp.Job_title.Replace("'", "''")}',
                                '{emp.Mail.Replace("'", "''")}',
                                '{emp.Works_from.Replace("'", "''")}',
                                '{emp.Integration_type}');";

                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            connection.Open();
                            command.ExecuteNonQuery();
                            connection.Close();
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                log.LogInformation($"Something went wrong: {ex}");
            }
        }
    }
}
