using System;

namespace EmployeeDataWarehouseIntegration.Models
{
    public class RawEmployee
    {
        public int Person_id { get; set; }
        public string First_name { get; set; }
        public string Last_name { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; }
        public string Job_title { get; set; }
        public string Company_name { get; set; }
        public DateTime Contract_until { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string Postal_code { get; set; }
        public string Street_name { get; set; }
        public string Street_number { get; set; }
        public bool Works_from_office { get; set; }
        public string Main_office { get; set; }

    }
}
