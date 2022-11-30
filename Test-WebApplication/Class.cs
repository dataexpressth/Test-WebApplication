using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace ConsoleApp1
{
    internal class Program
    {
        class JSMCustomer
        {
            public string AccountId { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public string OrganizationId { get; set; }
            public string LocationCode { get; set; }
        }

        class JSMOrganization
        {
            public string Id { get; set; }
            public string LocationCode { get; set; }
            public string Name { get; set; }
        }

        static void Main(string[] args)
        {
            bool execute = false;
            Boolean.TryParse(args[1], out execute);
            string filepath = args[0];

            Console.WriteLine("Opening Customer list file: {0}", filepath);
            Console.WriteLine("RUNMODE: {0}", execute);

            //string filepath = @"C:\Users\PawaratKitmanomai\OneDrive - Data Express Co Ltd\Desktop\CWN BU8 UAT\uat_test.csv"; 
            string cs = "Host=satao.db.elephantsql.com;Username=ofmjamer;Password=r0v3_xoVAdyCx-QxgbqxNRDKhLozQFjo;Database=ofmjamer";

            Dictionary<string, JSMOrganization> orgs = new Dictionary<string, JSMOrganization>();

            // Read Organization from DB
            using (var con = new NpgsqlConnection(cs))
            {
                con.Open();

                string sql = "SELECT id, province_code || district_code, name FROM organization_list";
                using var cmd = new NpgsqlCommand(sql, con);

                using NpgsqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    JSMOrganization o = new JSMOrganization()
                    {
                        Id = rdr.GetString(0),
                        LocationCode = rdr.GetString(1),
                        Name = rdr.GetString(2)
                    };

                    orgs.Add(o.LocationCode, o);
                    //Console.WriteLine("{0} {1} {2}", o.Id, o.LocationCode, o.Name);
                }
            }

            List<JSMCustomer> customers = new List<JSMCustomer>();

            // Read CSV file
            Console.WriteLine("Reading File: " + filepath);
            using (TextFieldParser csvParser = new TextFieldParser(filepath))
            {
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;

                // Skip the row with the column names
                Console.WriteLine(csvParser.ReadLine());

                while (!csvParser.EndOfData)
                {
                    // Read current line fields, pointer moves to the next line.
                    string[] fields = csvParser.ReadFields();

                    JSMCustomer c = new JSMCustomer()
                    {
                        Name = fields[0],
                        Email = fields[1],
                        LocationCode = fields[2] + fields[3],
                        OrganizationId = orgs[fields[2] + fields[3]].Id
                    };
                    customers.Add(c);
                    Console.WriteLine("{0} {1} {2} {3}", c.Name, c.Email, c.LocationCode, c.OrganizationId);
                }
            }

            // Create Customer and store ID
            RestClient client;
            RestRequest request;

            ///Test
            //RestClient client_x = new RestClient("https://chanwanichbu8.atlassian.net/rest/api/3/groupuserpicker?query=" + HttpUtility.UrlEncode(customers[0].Email));
            //client_x.Timeout = -1;
            //RestRequest request_x = new RestRequest(Method.GET);
            //request_x.AddHeader("Authorization", "Basic cGF3YXJhdEBkYXRhZXhwcmVzcy5jby50aDpzV3M4RGVtOTBKREV4WkVkTW5XdDExRTQ=");
            //IRestResponse response_x = client_x.Execute(request_x);

            //dynamic respond_query = JsonConvert.DeserializeObject(response_x.Content);
            //Console.WriteLine(respond_query);
            ///Test

            client = new RestClient("https://chanwanichbu8.atlassian.net/rest/servicedeskapi/customer");
            client.Timeout = -1;
            request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", "Basic cGF3YXJhdEBkYXRhZXhwcmVzcy5jby50aDpzV3M4RGVtOTBKREV4WkVkTW5XdDExRTQ=");
            request.AddHeader("Content-Type", "application/json");

            dynamic add_data = new JObject(
                new JProperty("displayName"),
                new JProperty("email"));

            foreach (JSMCustomer c in customers)
            {
                add_data.displayName = c.Name;
                add_data.email = c.Email;

                request.AddParameter("application/json", add_data, ParameterType.RequestBody);

                Console.WriteLine("Creating JSM Customer...");
                Console.WriteLine(add_data);

                if (execute)
                {
                    IRestResponse response = client.Execute(request);
                    Console.WriteLine("RESULT: " + (int)response.StatusCode + '-' + response.StatusCode);

                    if (response.StatusCode == System.Net.HttpStatusCode.Created)
                    {
                        dynamic respond_add = JsonConvert.DeserializeObject(response.Content);
                        c.AccountId = respond_add.accountId.ToString();
                        Console.WriteLine("Created JSM Customer: {0} with {1}", c.Email, c.AccountId);
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        RestClient client_x = new RestClient("https://chanwanichbu8.atlassian.net/rest/api/3/groupuserpicker?query=" + HttpUtility.UrlEncode(c.Email));
                        client_x.Timeout = -1;
                        RestRequest request_x = new RestRequest(Method.GET);
                        request_x.AddHeader("Authorization", "Basic cGF3YXJhdEBkYXRhZXhwcmVzcy5jby50aDpzV3M4RGVtOTBKREV4WkVkTW5XdDExRTQ=");
                        IRestResponse response_x = client_x.Execute(request_x);

                        dynamic respond_query = JsonConvert.DeserializeObject(response_x.Content);

                        //if (respond_query.users.total)
                        Console.WriteLine(respond_query.users.total);
                        Console.WriteLine(respond_query.users.users[0].accountId);

                        c.AccountId = respond_query.users.users[0].accountId.ToString();
                    }
                }
            }

            // Add Customer to Service Desk Project
            Console.WriteLine("Adding JSM Customer to Service Desk Project...");
            {
                JArray jarrayObj = new JArray();
                //bool checkAddCustomer = false;

                foreach (JSMCustomer c in customers)
                {

                    //Console.WriteLine("{0} {1} {2} {3}", c.Name, c.Email, c.LocationCode, c.OrganizationId);
                    if (c.AccountId != null)
                    {
                        jarrayObj.Add((c.AccountId != null) ? c.AccountId : c.Email);
                        //checkAddCustomer = true;
                    }
                    else
                    {
                        Console.WriteLine("No accountId for: {0}, skipping..", c.Email);
                        continue;
                    }
                }
                JObject update_data = new JObject(new JProperty("accountIds", jarrayObj));

                client = new RestClient("https://chanwanichbu8.atlassian.net/rest/servicedeskapi/servicedesk/7/customer");
                client.Timeout = -1;
                request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", "Basic cGF3YXJhdEBkYXRhZXhwcmVzcy5jby50aDpzV3M4RGVtOTBKREV4WkVkTW5XdDExRTQ=");
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", update_data, ParameterType.RequestBody);


                Console.WriteLine(update_data);
                if (execute)
                {
                    //if (checkAddCustomer)
                    {
                        IRestResponse response = client.Execute(request);
                        Console.WriteLine("RESULT: " + (int)response.StatusCode + '-' + response.StatusCode);
                        Console.WriteLine(response.Content);
                    }
                    //else
                    //Console.WriteLine("Skipped all customer");
                }
            }

            
        }
    }
}