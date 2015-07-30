using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication.Models
{
    public class SensorsMode
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public int? PollingInterval { get; set; }
        public string WebHost { get; set; }
        public int? WebPort { get; set; }
        public string WebPath { get; set; }
    }
}