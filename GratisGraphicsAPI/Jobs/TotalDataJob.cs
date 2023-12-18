using GratisGraphicsAPI.Controllers;
using Quartz;

namespace GratisGraphicsAPI.Jobs
{
    public class TotalDataJob : IJob
    {
        private readonly ITableService _tableService;
        public TotalDataJob(ITableService tableService)
        {
            _tableService = tableService;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            _tableService.GetTableSizes();
        }

    }
}
