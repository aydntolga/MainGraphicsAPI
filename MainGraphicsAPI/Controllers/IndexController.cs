using MainGraphicsAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Xml.Linq;

namespace MainGraphicsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IndexController:Controller
    {
        private readonly MyDbContext _context;
        public IndexController(MyDbContext context)
        {
            _context = context;
        }
        // Get Top 10 Most Used Indexes
        [HttpGet("/mostindex")]
        public async Task<IActionResult> GetMostUsedIndexes()
        {
            var indexSizes = new List<IndexSize>();

            using (var connection = new SqlConnection("Server=DESKTOP-JGFNNVS; Database=Northwind;Trusted_Connection=True;"))
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
        public async Task<IActionResult> GetFragmentedIndexes()
        {
            var fragmentedIndexes = new List<FragmentedIndex>();

            try
            {
                using (var connection = new SqlConnection("Server=localhost; Database=Northwind;Trusted_Connection=True;"))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("SELECT name, avg_fragmentation_in_percent FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, NULL) AS ps JOIN sys.indexes AS i ON ps.[object_id] = i.[object_id] AND ps.index_id = i.index_id WHERE ps.avg_fragmentation_in_percent >= 30", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var fragmentedIndex = new FragmentedIndex
                                {
                                    IndexName = reader.GetString(0),
                                    FragmentationRatio = reader.GetDouble(1)
                                };

                                fragmentedIndexes.Add(fragmentedIndex);
                            }
                        }
                    }
                }

                return Ok(fragmentedIndexes);
            }
            catch (Exception ex)
            {
                // Log the detailed exception message
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }







    }
}
