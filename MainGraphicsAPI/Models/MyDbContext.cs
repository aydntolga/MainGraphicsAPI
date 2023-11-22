using Microsoft.EntityFrameworkCore;

namespace MainGraphicsAPI.Models
{
    public class MyDbContext:DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        {

        }
        public DbSet<IndexSize> IndexSizes { get; set; }
        public DbSet<TotalTableSize> TotalTableSizes { get; set; }
    }
}
