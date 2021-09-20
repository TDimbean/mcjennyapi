using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace McJenny.IntegrationTests
{
    [TestClass]
    public class EmployeeApiIntegrationTests
    {
        [TestMethod]
        public async Task GetEmployees()
        {
            using (var client = new TestClientProvider().Client)
            {
                var response = await client.GetAsync("/api/employees");

                response.EnsureSuccessStatusCode();

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            }
        }
    }
}
