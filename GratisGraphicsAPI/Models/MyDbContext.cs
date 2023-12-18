using Microsoft.EntityFrameworkCore;

namespace GratisGraphicsAPI.Models
{
    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        {

        }
        public DbSet<TotalTableSize> TotalTableSizes { get; set; }
        public DbSet<IndexSize> IndexSizes { get; set; }
    }
}
