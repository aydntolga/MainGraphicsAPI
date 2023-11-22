using MainGraphicsAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlClient;

namespace MainGraphicsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TablesController : Controller
    {
        private readonly MyDbContext _context;

        public TablesController(MyDbContext context)
        {
            _context = context;
        }
    
        [HttpGet("table")]
        public ActionResult<IEnumerable<TotalTableSize>> GetTableSizes()
        {
            var tableSizes = new List<TotalTableSize>();

            using (var connection = new SqlConnection("Server=DESKTOP-JGFNNVS;Database=Northwind;Trusted_Connection=True;"))
            {
                connection.Open();

                using (var command = new SqlCommand("CREATE TABLE #tbl(name nvarchar(128), rows varchar(50), reserved varchar(50), data varchar(50), index_size varchar(50), unused varchar(50))", connection))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new SqlCommand("EXEC sp_msforeachtable 'INSERT INTO #tbl EXEC sp_spaceused [?]'", connection))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new SqlCommand("SELECT TOP 10 name as 'TableName', CONVERT(INT, SUBSTRING(data, 1, LEN(data)-3)) / 1024.0 / 1024.0 as 'Table Size (GB)' FROM #tbl ORDER BY CONVERT(INT, SUBSTRING(data, 1, LEN(data)-3)) DESC", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var tableSize = new TotalTableSize
                            {
                                TableName = reader.GetString(0),
                                TotalTableSizeGB = reader.GetDecimal(1)
                            };

                            tableSizes.Add(tableSize);
                        }
                    }
                }

                using (var command = new SqlCommand("DROP TABLE #tbl", connection))
                {
                    command.ExecuteNonQuery();
                }
            }

            return Ok(tableSizes);
        }
        
            [HttpGet("GetIndexSizes")]
            public ActionResult<IEnumerable<IndexSize>> GetIndexSizes()
            {
                List<IndexSize> indexSizes = new List<IndexSize>();

                // Connection string
                string connectionString = "Server=DESKTOP-JGFNNVS;Database=Northwind;Trusted_Connection=True;";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Tablo oluşturma
                    using (SqlCommand createTableCommand = new SqlCommand(@"
                    CREATE TABLE #tbl (
                        name nvarchar(128),
                        rows varchar(50),
                        reserved varchar(50),
                        data varchar(50),
                        index_size varchar(50),
                        unused varchar(50)
                    )", connection))
                    {
                        createTableCommand.ExecuteNonQuery();
                    }

                    // sp_msforeachtable ile #tbl'ye veri ekleme
                    using (SqlCommand insertDataCommand = new SqlCommand("exec sp_msforeachtable 'insert into #tbl exec sp_spaceused [?]'"))
                    {
                        insertDataCommand.Connection = connection;
                        insertDataCommand.ExecuteNonQuery();
                    }

                    // En büyük 10 index bilgisini alma
                    using (SqlCommand selectDataCommand = new SqlCommand(@"
                    SELECT TOP 10
                        name AS 'Index Name',
                        CONVERT(INT, SUBSTRING(index_size, 1, LEN(index_size) - 3)) / 1024.0 / 1024.0 AS 'Index Size (GB)'
                    FROM #tbl
                    ORDER BY CONVERT(INT, SUBSTRING(index_size, 1, LEN(index_size) - 3)) DESC", connection))
                    {
                        using (SqlDataReader reader = selectDataCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                IndexSize indexSize = new IndexSize
                                {
                                    IndexName = reader.GetString(0),
                                    IndexSizeGB = Convert.ToDouble(reader.GetDecimal(1)),
                                };

                                indexSizes.Add(indexSize);
                            }
                        }
                    }

                    // Geçici tabloyu silme
                    using (SqlCommand dropTableCommand = new SqlCommand("DROP TABLE #tbl", connection))
                    {
                        dropTableCommand.ExecuteNonQuery();
                    }
                }

                return Ok(indexSizes);
            }
       }
}
