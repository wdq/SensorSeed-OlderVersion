using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Linq;
using System.Data.Sql;

namespace WebApplication.Controllers
{
    public class MSSQLController : ApiController
    {
        // GET api/mssql
        public IEnumerable<string> Get()
        {
            //var database = new SensorDataLinqToSQLDataContext();
            //var stuff = database.Sensors.AsEnumerable();
            

            return new string[] { "value1", "value2" };
        }

        // GET api/mssql/5
        public string Get(int id)
        {
            return "value";
        }

        //public void 

        // POST api/mssql
        public void Post([FromBody]string value)
        {
        }

        // PUT api/mssql/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/mssql/5
        public void Delete(int id)
        {
        }
    }
}
