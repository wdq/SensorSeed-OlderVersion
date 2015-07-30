using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Linq;
using System.Data.Sql;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace WebApplication.Controllers
{
    public class MSSQLController : Controller
    {

        public ActionResult GetSensor(string query, string queryType)
        {
            var database = new SensorDataLinqToSQLDataContext();

            if (queryType == "Name")
            {
                Sensor sensor = database.Sensors.AsQueryable().FirstOrDefault(x => x.Name == query);

                return Content(sensor.Name);
            }

            if (queryType == "Id")
            {
                Sensor sensor = database.Sensors.AsQueryable().FirstOrDefault(x => x.Id == new Guid(query));

                return Content(sensor.Name);
            }

            return null;
        }
        public class AllowCrossSiteJsonAttribute : ActionFilterAttribute
        {
            public override void OnActionExecuting(ActionExecutingContext filterContext)
            {
                filterContext.RequestContext.HttpContext.Response.AddHeader("Access-Control-Allow-Origin", "*");
                base.OnActionExecuting(filterContext);
            }
        }

        [AllowCrossSiteJson]
        public ActionResult GetSensorData(string sensorType, string sensorId, string startTime, string endTime)
        {

            var database = new SensorDataLinqToSQLDataContext();

            BsonArray result = new BsonArray();

            if (sensorType == "Temperature")
            {
                List<TemperatureData> temperatureDatas = database.TemperatureDatas
                    .AsQueryable()
                    .Where(x => x.SensorId == new Guid(sensorId))
                    .Where(x => (x.Timestamp - new DateTime(1970, 1, 1)).TotalSeconds > int.Parse(startTime))
                    .Where(x => (x.Timestamp - new DateTime(1970, 1, 1)).TotalSeconds < int.Parse(endTime))
                    .OrderBy(o => o.Timestamp)
                    .ToList();

                foreach (var temperatureData in temperatureDatas)
                {
                    BsonDocument document = new BsonDocument
                    { 
                    {"Timestamp", (temperatureData.Timestamp - new DateTime(1970, 1, 1)).TotalSeconds},
                    {"Value", temperatureData.Value.ToString()}
                    };
                    result.Add(document);
                }

            }
            if (sensorType == "Humidity")
            {
                List<HumidityData> humidityDatas = database.HumidityDatas.AsQueryable().Where(x => x.SensorId == new Guid(sensorId)).ToList();
            }

            return Content(result.ToJson(), "application/json");
        }
  
        public ActionResult Test()
        {
            return null;
        }


    }
}
