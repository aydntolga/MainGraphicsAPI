using GratisGraphicsAPI.Models;

namespace GratisGraphicsAPI.Controllers
{
    public interface ITableService
    {
        List<TotalTableSize> GetTableSizes();
        List<IndexSize> GetIndexSizes();
    }
}
