using APIAdventureWorks.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace APIAdventureWorks.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class IndexController : Controller
    {
        private readonly MyDbContext _context;

        public IndexController(MyDbContext context)
        {
            _context = context;
        }

        private string GetDatabaseConnectionString()
        {
            try
            {
                // IConfiguration nesnesini yaratın
                var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                IConfigurationRoot configuration = configBuilder.Build();

                // Bağlantı dizesini al
                var databaseConnectionString = configuration.GetConnectionString("DatabaseAdventureWorks");

                if (string.IsNullOrEmpty(databaseConnectionString))
                {
                    // Eğer bağlantı dizesi tanımlı değilse hata fırlatın
                    throw new InvalidOperationException("Database connection string not found in appsettings.json.");
                }

                return databaseConnectionString;
            }
            catch (Exception ex)
            {
                // Loglama yapabilir veya hata işleme stratejilerinizi burada uygulayabilirsiniz
                Console.WriteLine($"Error getting database connection string: {ex.Message}");
                throw; // Hata fırlatılır, bu da uygulamanın başarısız olmasına neden olur
            }
        }


        [HttpGet("/mostindex")]
        public async Task<IActionResult> GetMostUsedIndexes()
        {
            var indexSizes = new List<IndexSize>();

            using (var connection = new SqlConnection(GetDatabaseConnectionString()))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("CREATE TABLE #tbl(name nvarchar(128), rows varchar(50), reserved varchar(50), data varchar(50), index_size varchar(50), unused varchar(50))", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                using (var command = new SqlCommand("EXEC sp_msforeachtable 'INSERT INTO #tbl EXEC sp_spaceused [?]'", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                using (var command = new SqlCommand("DROP TABLE #tbl", connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }

            return Ok(indexSizes);
        }

        [HttpGet("/fragmentedindex")]
        public List<FragmentedIndex> GetFragmentedIndexes()
        {
            var fragmentedIndexes = new List<FragmentedIndex>();

            // Connection string
            string connectionString = GetDatabaseConnectionString();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand selectDataCommand = new SqlCommand(@"
            SELECT name AS 'Index Name',
                   avg_fragmentation_in_percent AS 'Fragmentation Ratio'
            FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, NULL) AS ps 
            JOIN sys.indexes AS i ON ps.[object_id] = i.[object_id] AND ps.index_id = i.index_id 
            WHERE ps.avg_fragmentation_in_percent >= 30", connection))
                {
                    using (SqlDataReader reader = selectDataCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            FragmentedIndex fragmentedIndex = new FragmentedIndex();

                            // Check if the value is DBNull for IndexName
                            if (!reader.IsDBNull(0))
                            {
                                fragmentedIndex.IndexName = reader.GetString(0);
                            }
                            else
                            {
                                // Handle DBNull, set a default value or handle it according to your logic
                                fragmentedIndex.IndexName = "Unknown"; // Or any default value you want
                            }

                            // Check if the value is DBNull for FragmentationRatio
                            if (!reader.IsDBNull(1))
                            {
                                fragmentedIndex.FragmentationRatio = reader.GetDouble(1);
                            }
                            else
                            {
                                // Handle DBNull, set a default value or handle it according to your logic
                                fragmentedIndex.FragmentationRatio = 0.0; // Or any default value you want
                            }

                            fragmentedIndexes.Add(fragmentedIndex);
                        }
                    }
                }
            }

            return fragmentedIndexes;
        }
    }

}

