using APIAdventureWorks.Models;

namespace APIAdventureWorks.Controllers
{
    public interface ITableService
    {
        List<TotalTableSize> GetTableSizes();
        List<IndexSize> GetIndexSizes();
    }
}
