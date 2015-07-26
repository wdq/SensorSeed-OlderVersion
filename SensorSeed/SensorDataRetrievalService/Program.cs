using System;
using System.Collections.Generic;
using SkyLinq.Linq;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging.Simple;
using Quartz;
using Quartz.Impl;
using System.Data.Sql;
using System.IO;
using WebApplication;
using System.Net;

namespace SensorDataRetrievalService
{
    class Program
    {

        public static List<KeyValuePair<string, JobKey>> JobKeys;
        public static IScheduler scheduler;

        static void Main(string[] args)
        {
            try
            {
                JobKeys = new List<KeyValuePair<string, JobKey>>();

                var database = new SensorDataLinqToSQLDataContext();

                List<Sensor> sensors = database.Sensors.AsQueryable().Where(x => x.Active).ToList();

                Common.Logging.LogManager.Adapter = new Common.Logging.Simple.ConsoleOutLoggerFactoryAdapter {Level = Common.Logging.LogLevel.Info};

                scheduler = StdSchedulerFactory.GetDefaultScheduler();

                scheduler.Start();
                

                IJobDetail configChangesJob = JobBuilder.Create<ConfigChangesJob>()
                    .WithIdentity("ConfigChanges", "ConfigChangesGroup")
                    .Build();

                ITrigger configChangesTrigger = TriggerBuilder.Create()
                    .WithIdentity("ConfigChanges", "ConfigChangesGroup")
                    .StartNow()
                    .WithSimpleSchedule(x => x
                        .WithIntervalInMinutes(10)
                        .RepeatForever())
                    .Build();

                scheduler.ScheduleJob(configChangesJob, configChangesTrigger);

                JobKeys.Add(new KeyValuePair<string, JobKey>("ConfigChanges", configChangesJob.Key));

                foreach (Sensor sensor in sensors)
                {
                    int pollingInterval = sensor.PollingInterval ?? default(int);

                    IJobDetail sensorJob = JobBuilder.Create<SensorJob>()
                        .WithIdentity(sensor.Id.ToString(), "SensorGroup")
                        .UsingJobData("Id", sensor.Id.ToString())
                        .UsingJobData("Name", sensor.Name)
                        .UsingJobData("Type", sensor.Type)
                        .UsingJobData("PollingInterval", sensor.PollingInterval.ToString())
                        .UsingJobData("WebHost", sensor.WebHost)
                        .UsingJobData("WebPort", sensor.WebPort.ToString())
                        .UsingJobData("WebPath", sensor.WebPath)
                        .Build();

                    ITrigger sensorTrigger = TriggerBuilder.Create()
                        .WithIdentity(sensor.Id.ToString(), "SensorGroup")
                        .StartNow()
                        .WithSimpleSchedule(x => x
                            .WithIntervalInSeconds(pollingInterval)
                            .RepeatForever())
                        .Build();

                    scheduler.ScheduleJob(sensorJob, sensorTrigger);

                    JobKeys.Add(new KeyValuePair<string, JobKey>(sensor.Id.ToString(), sensorJob.Key));
                }

            }
            catch (SchedulerException se)
            {
                Console.WriteLine(se);
            }

            Console.WriteLine("Press any key to close the application");
            Console.ReadKey();

        }

        public class ConfigChangesJob : IJob
        {
            public void Execute(IJobExecutionContext context)
            {
                var database = new SensorDataLinqToSQLDataContext();

                List<Sensor> sensors = database.Sensors.AsQueryable().Where(x => x.Changed).ToList();

                foreach (Sensor sensor in sensors)
                {
                    if (sensor.ToRemove)
                    {
                        KeyValuePair<string, JobKey> JobKey = JobKeys.SingleOrDefault(x => x.Key == sensor.Id.ToString());

                        if (JobKey.Value != null)
                        {
                            scheduler.DeleteJob(JobKey.Value);
                            JobKeys.Remove(JobKey);
                        }

                        database.Sensors.DeleteOnSubmit(sensor);

                        if (sensor.Type == "Temperature")
                        {
                            List<TemperatureData> temperatureData = database.TemperatureDatas.AsQueryable().Where(x => x.SensorId == sensor.Id).ToList();

                            database.TemperatureDatas.DeleteAllOnSubmit(temperatureData);
                        }

                        if (sensor.Type == "Humidity")
                        {
                            List<HumidityData> humidityData = database.HumidityDatas.AsQueryable().Where(x => x.SensorId == sensor.Id).ToList();

                            database.HumidityDatas.DeleteAllOnSubmit(humidityData);
                        }
                    }

                    if (sensor.ActiveChanged)
                    {
                        Console.WriteLine("Active has been changed.");
                        if (!sensor.Active)
                        {
                            Console.WriteLine("Removing...");
                            KeyValuePair<string, JobKey> JobKey = JobKeys.SingleOrDefault(x => x.Key == sensor.Id.ToString());
                            if (JobKey.Value != null)
                            {
                                scheduler.DeleteJob(JobKey.Value);
                                JobKeys.Remove(JobKey);
                            }
                            sensor.ActiveChanged = false;
                            sensor.Active = false;

                        } else {
                            Console.WriteLine("Adding...");
                            int pollingInterval = sensor.PollingInterval ?? default(int);

                            IJobDetail sensorJob = JobBuilder.Create<SensorJob>()
                                .WithIdentity(sensor.Id.ToString(), "SensorGroup")
                                .UsingJobData("Id", sensor.Id.ToString())
                                .UsingJobData("Name", sensor.Name)
                                .UsingJobData("Type", sensor.Type)
                                .UsingJobData("PollingInterval", sensor.PollingInterval.ToString())
                                .UsingJobData("WebHost", sensor.WebHost)
                                .UsingJobData("WebPort", sensor.WebPort.ToString())
                                .UsingJobData("WebPath", sensor.WebPath)
                                .Build();

                            ITrigger sensorTrigger = TriggerBuilder.Create()
                                .WithIdentity(sensor.Id.ToString(), "SensorGroup")
                                .StartNow()
                                .WithSimpleSchedule(x => x
                                    .WithIntervalInSeconds(pollingInterval)
                                    .RepeatForever())
                                .Build();

                            scheduler.ScheduleJob(sensorJob, sensorTrigger);

                            JobKeys.Add(new KeyValuePair<string, JobKey>(sensor.Id.ToString(), sensorJob.Key));

                            sensor.ActiveChanged = false;
                            sensor.Active = true;
                        }
                    }

                    sensor.Changed = false;
                    database.SubmitChanges();
                }
                
            }
        }


        public class SensorJob : IJob
        {
            public void Execute(IJobExecutionContext context)
            {
                JobKey key = context.JobDetail.Key;
                JobDataMap dataMap = context.JobDetail.JobDataMap;

                Sensor sensor = new Sensor();
                sensor.Id = new Guid(dataMap.GetString("Id"));
                sensor.Name = dataMap.GetString("Name");
                sensor.PollingInterval = Int32.Parse(dataMap.GetString("PollingInterval"));
                sensor.Type = dataMap.GetString("Type");
                sensor.WebHost = dataMap.GetString("WebHost");
                sensor.WebPath = dataMap.GetString("WebPath");
                sensor.WebPort = Int32.Parse(dataMap.GetString("WebPort"));

                DateTime now = DateTime.UtcNow;

                string url = "http://" + sensor.WebHost + ":" + sensor.WebPort + sensor.WebPath;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse) request.GetResponse();
                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                string data = reader.ReadToEnd();

                var database = new SensorDataLinqToSQLDataContext();

                if (sensor.Type == "Temperature")
                {
                    decimal value = Decimal.Parse(data);

                    TemperatureData temperature = new TemperatureData();
                    temperature.Id = Guid.NewGuid();
                    temperature.SensorId = sensor.Id;
                    temperature.Timestamp = DateTime.Now;
                    temperature.Value = value;

                    database.TemperatureDatas.InsertOnSubmit(temperature);
                    database.SubmitChanges();
                }

                if (sensor.Type == "Humidity")
                {
                    decimal value = Decimal.Parse(data);

                    HumidityData humidity = new HumidityData();
                    humidity.Id = Guid.NewGuid();
                    humidity.SensorId = sensor.Id;
                    humidity.Timestamp = DateTime.UtcNow;
                    humidity.Value = value;

                    database.HumidityDatas.InsertOnSubmit(humidity);
                    database.SubmitChanges();
                }

                //List<Sensor> sensors = database.Sensors.AsEnumerable().ToList();


                Console.WriteLine("Hello from " + sensor.Name + " at " + now + " Data: " + data);
            }
        }

    }
}
