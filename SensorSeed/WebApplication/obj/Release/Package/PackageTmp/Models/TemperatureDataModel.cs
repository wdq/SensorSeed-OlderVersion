using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication.Models
{
    public class TemperatureDataModel
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal Value { get; set; }
        public Guid SensorId { get; set; }
    }
}