using APIAdventureWorks.Controllers;
using APIAdventureWorks.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace APIAdventureWorks.Services
{
    public class TableServices : ITableService
    {
        private readonly IConfiguration _configuration;

        public TableServices(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetDatabaseConnectionString()
        {
            try
            {
                // Bağlantı dizesini appsettings.json'dan al
                var databaseConnectionString = _configuration.GetConnectionString("DatabaseAdventureWorks");

                if (string.IsNullOrEmpty(databaseConnectionString))
                {
                    // Eğer bağlantı dizesi tanımlı değilse hata fırlat
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

        public List<IndexSize> GetIndexSizes()
        {
            List<IndexSize> indexSizes = new List<IndexSize>();

            // Connection string
            string connectionString = GetDatabaseConnectionString();

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

            return indexSizes;
        }

        public List<TotalTableSize> GetTableSizes()
        {
            var tableSizes = new List<TotalTableSize>();
            string connectionString = GetDatabaseConnectionString();

            using (var connection = new SqlConnection(connectionString))
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

            return tableSizes;
        }

    }
}
