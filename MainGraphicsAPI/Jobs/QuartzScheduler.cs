using Quartz;
using Quartz.Impl;

namespace MainGraphicsAPI.Jobs
{
    public class QuartzScheduler
    {
        public static void Start()
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
                    s.OnMondayThroughFriday()
                     .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(1, 0))
                     .EndingDailyAt(TimeOfDay.HourAndMinuteOfDay(2, 0))
                     .WithIntervalInMinutes(15)
                )
                .Build();

            scheduler.ScheduleJob(job, trigger);
            scheduler.Start().Wait();
        }
    }
}
