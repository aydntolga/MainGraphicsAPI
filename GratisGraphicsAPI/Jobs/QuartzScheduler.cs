using Quartz.Impl;
using Quartz;

namespace GratisGraphicsAPI.Jobs
{
    public class QuartzScheduler
    {
        public static async void Start()
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            IScheduler scheduler = schedulerFactory.GetScheduler().Result;

            IJobDetail job = JobBuilder.Create<TotalDataJob>()
                .WithIdentity("job1", "group1")
                .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("trigger1", "group1")
                .WithDailyTimeIntervalSchedule
                (s =>
                    s.WithIntervalInSeconds(10)
                )
                .Build();

            scheduler.ScheduleJob(job, trigger);
            scheduler.Start();
            scheduler.Shutdown();


        }
    }
}
