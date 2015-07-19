using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;

namespace SensorDataRetrievalService
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Common.Logging.LogManager.Adapter = new Common.Logging.Simple.ConsoleOutLoggerFactoryAdapter {Level = Common.Logging.LogLevel.Info};

                IScheduler scheduler = StdSchedulerFactory.GetDefaultScheduler();

                scheduler.Start();

                IJobDetail WilliamRoomJob = JobBuilder.Create<WilliamRoomJob>()
                    .WithIdentity("WilliamRoomJob", "FiveMinuteGroup")
                    .Build();

                IJobDetail ServerRoomJob = JobBuilder.Create<ServerRoomJob>()
                    .WithIdentity("ServerRoomJob", "FiveMinuteGroup")
                    .Build();

                ITrigger FiveMinuteTrigger = TriggerBuilder.Create()
                    .WithIdentity("FiveMinuteTrigger", "FiveMinuteGroup")
                    .StartNow()
                    .WithSimpleSchedule(x => x
                        .WithIntervalInMinutes(5)
                        .RepeatForever())
                    .Build();

                scheduler.ScheduleJob(WilliamRoomJob, FiveMinuteTrigger);
                scheduler.ScheduleJob(ServerRoomJob, FiveMinuteTrigger);

                Thread.Sleep(TimeSpan.FromSeconds(60));

                scheduler.Shutdown();
            }
            catch (SchedulerException se)
            {
                Console.WriteLine(se);
            }

            Console.WriteLine("Press any key to close the application");
            Console.ReadKey();

        }

        public class HelloJob : IJob
        {
            public void Execute(IJobExecutionContext context)
            {
                Console.WriteLine("Greetings from HelloJob!");
            }
        }

        public class WilliamRoomJob : IJob
        {
            public void Execute(IJobExecutionContext context)
            {
                Console.WriteLine("William's Room Job...");
            }
        }

        public class ServerRoomJob : IJob
        {
            public void Execute(IJobExecutionContext context)
            {
                Console.WriteLine("Server Room Job...");
            }
        }

    }
}
