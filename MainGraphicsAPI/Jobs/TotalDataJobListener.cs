using Quartz;

namespace MainGraphicsAPI.Jobs
{
    public class TotalDataJobListener : IJobListener
    {
        public string Name => "YourJobListener";

        public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            Console.WriteLine($"Job {context.JobDetail.Key} execution vetoed.");
            return Task.CompletedTask;
        }

        public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            Console.WriteLine($"Job {context.JobDetail.Key} to be executed.");
            return Task.CompletedTask;
        }

        public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default(CancellationToken))
        {
            Console.WriteLine($"Job {context.JobDetail.Key} was executed.");
            return Task.CompletedTask;
        }
    }
}
