using MainGraphicsAPI.Models;

namespace MainGraphicsAPI.Controllers
{
    public interface ITableService
    {
        List<TotalTableSize> GetTableSizes();
        List<IndexSize> GetIndexSizes();
    }
}
