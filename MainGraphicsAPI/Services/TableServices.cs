using MainGraphicsAPI.Controllers;
using MainGraphicsAPI.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace MainGraphicsAPI.Services
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
                var databaseConnectionString = _configuration.GetConnectionString("DatabaseConnectionNorthwind");

                if (string.IsNullOrEmpty(databaseConnectionString))
                {
                    throw new InvalidOperationException("Database connection string not found in appsettings.json.");
                }

                return databaseConnectionString;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting database connection string: {ex.Message}");
                throw;
            }
        }

        public List<IndexSize> GetIndexSizes()
        {
            List<IndexSize> indexSizes = new List<IndexSize>();

            using (SqlConnection connection = new SqlConnection(GetDatabaseConnectionString()))
            {
                connection.Open();

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

                using (SqlCommand insertDataCommand = new SqlCommand("exec sp_msforeachtable 'insert into #tbl exec sp_spaceused [?]'", connection))
                {
                    insertDataCommand.Connection = connection;
                    insertDataCommand.ExecuteNonQuery();
                }

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

                using (SqlCommand dropTableCommand = new SqlCommand("DROP TABLE #tbl", connection))
                {
                    dropTableCommand.ExecuteNonQuery();
                }
            }

            return (indexSizes);
        }

        public List<TotalTableSize> GetTableSizes()
        {
            var tableSizes = new List<TotalTableSize>();
            using (var connection = new SqlConnection(GetDatabaseConnectionString()))
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

            return (tableSizes);
        }
    }
}
