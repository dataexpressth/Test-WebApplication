// Update Organization
            //Console.WriteLine(" == Search == ");
            foreach (JSMOrganization o in orgs.Values)
            {
                var queryCustomer = from c in customers
                                    where c.OrganizationId == o.Id
                                    select c;

                if (queryCustomer.Count() == 0)
                    continue;

                Console.WriteLine("Adding JSM Customer to Organizaion: {0} {1} {2}", o.Id, o.Name, o.LocationCode);

                JArray jarrayObj = new JArray();

                //bool checkAddOrg = false;

                foreach (JSMCustomer c in queryCustomer)
                {
                    //jarrayObj.Add((c.AccountId != null) ? c.AccountId : c.Email);
                    //Console.WriteLine("{0} {1} {2} {3}", c.Name, c.Email, c.LocationCode, c.OrganizationId);

                    if (c.AccountId != null)
                    {
                        jarrayObj.Add((c.AccountId != null) ? c.AccountId : c.Email);
                        //checkAddOrg = true;
                    }
                    else
                    {
                        Console.WriteLine("No accountId for: {0}, skipping..", c.Email);
                        continue;
                    }
                }
                JObject update_data = new JObject(new JProperty("accountIds", jarrayObj));
                Console.WriteLine(update_data);

                client = new RestClient("https://chanwanichbu8.atlassian.net/rest/servicedeskapi/organization/" + o.Id + "/user");
                client.Timeout = -1;
                request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", "Basic cGF3YXJhdEBkYXRhZXhwcmVzcy5jby50aDpzV3M4RGVtOTBKREV4WkVkTW5XdDExRTQ=");
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", update_data, ParameterType.RequestBody);

                //if (execute)
                {
                    //if (checkAddOrg)
                    {
                        IRestResponse response = client.Execute(request);
                        Console.WriteLine("RESULT: " + (int)response.StatusCode + '-' + response.StatusCode);
                        Console.WriteLine(response.Content);
                    }
                    //else
                    //Console.WriteLine("Skipped all customer");
                }
            }
