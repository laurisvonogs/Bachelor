using System;
using System.Collections.Generic;
using EmployeeIntegration.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;

namespace EmployeeIntegration
{
    public static class ComposeEmployees
    {
        [FunctionName("ComposeEmployees")]
        public static void Run([TimerTrigger("3 0 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var rawEmployees = GetRawEmployees();
            log.LogInformation("Retrieved Employees from input system");

            var composedEmployees = ComposeRawEmployees(rawEmployees);
            log.LogInformation("Composed Employees");

            PostEmployees(composedEmployees);
            log.LogInformation("Sent Employees to output system");
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
        private static void PostEmployees(object composedEmployees)
        {
            var request = new RestRequest(Method.POST);
            request.AddHeader("x-apikey", Environment.GetEnvironmentVariable("OUTPUT_API_KEY"));
            request.AddHeader("Content-Type", "application/json");
            request.AddJsonBody(composedEmployees);

            var outputApiUrl = Environment.GetEnvironmentVariable("OUTPUT_API_URL");
            var postEmployeesURL = new RestClient($"{outputApiUrl}/collection");
            postEmployeesURL.Execute(request);
        }
    }
}
