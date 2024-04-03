using CentralWriter.Infos;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Timers;
using Microsoft.Extensions.Configuration;


namespace CentralWriter
{
    public partial class Service1 : ServiceBase
    {
        private Timer timer;
        private IConfigurationRoot configuration;
        private string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logfile.txt");

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);


            configuration = builder.Build();

            timer = new Timer();
            timer.Elapsed += new ElapsedEventHandler(OnTimerElapsed);
            timer.Interval = 60 * 1000; 
            timer.Enabled = true;

            Log("Service started.");
        }


        protected override void OnStop()
        {
            timer.Enabled = false;
            Log("Service stopped");
        }
        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            SyncAllDatabases();
        }
        private void SyncAllDatabases()
        {
            Log("Syncing all databases...");
            using (SqlConnection centralConnection = new SqlConnection(configuration.GetConnectionString("CentralDatabase")))
            {
                centralConnection.Open();
                foreach(var databaseInfo in GetDatabaseInfo())
                {
                    SyncDatabaseData(databaseInfo, centralConnection);
                }
                centralConnection.Close();
            }
            Log("Sync Completed");
        }

        private void SyncDatabaseData(DatabaseInfo databaseInfo, SqlConnection destinationConnection)
        {
            Log($"Syncing data for database: {databaseInfo.Name}");

            using (SqlConnection sourceConnection = new SqlConnection(databaseInfo.ConnectionString))
            {
                sourceConnection.Open();

                foreach (var tableInfo in databaseInfo.Tables)
                {
                    SyncTableData(databaseInfo.Name, tableInfo, sourceConnection, destinationConnection);
                }

                sourceConnection.Close();
            }

            Log($"Data sync for database {databaseInfo.Name} completed.");
        }
        private List<DatabaseInfo> GetDatabaseInfo()
        {
            List<DatabaseInfo> databaseInfos = new List<DatabaseInfo>
            {
                GetDatabaseInfoFor("AdventureWorks"),


                GetDatabaseInfoFor("Northwind"),


                GetDatabaseInfoFor("Gratis"),

            };

            return databaseInfos;
        }

        private DatabaseInfo GetDatabaseInfoFor(string databaseName)
        {
            string connectionString = configuration.GetConnectionString(databaseName);

            // Tablo isimlerini almak için gerekli SQL sorgusu
            string query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";

            List<TableInfo> tables = new List<TableInfo>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string tableName = reader.GetString(0);

                            // Tablo bilgilerini oluştur
                            TableInfo tableInfo = new TableInfo
                            {
                                Name = tableName,
                                TotalTableSizeColumn = "totalTableSizeGB",
                                TotalIndexSizeColumn = "totalIndexSizesGB",
                                MostUsedIndexColumn = "mostUsedIndex"
                            };

                            tables.Add(tableInfo);
                        }
                    }
                }
            }

           
            return new DatabaseInfo
            {
                Name = databaseName,
                ConnectionString = connectionString,
                Tables = tables,
            };
        }
        private void SyncTableData(string databaseName, TableInfo tableInfo, SqlConnection sourceConnection, SqlConnection destinationConnection)
        {
            Log($"Syncing data for table: {tableInfo.Name} using source connection: {sourceConnection.ConnectionString}");

            try
            {
                var tableSizes = new List<TotalTableSize>();
                List<IndexSize> indexSizes = new List<IndexSize>(); // indexSizes değişkenini tanımla

                // Tablo boyutları sorgusu
                using (var command = new SqlCommand("CREATE TABLE #tbl(name nvarchar(128), rows varchar(50), reserved varchar(50), data varchar(50), index_size varchar(50), unused varchar(50))", sourceConnection))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new SqlCommand("EXEC sp_msforeachtable 'INSERT INTO #tbl EXEC sp_spaceused [?]'", sourceConnection))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new SqlCommand("SELECT name as 'TableName', CONVERT(INT, SUBSTRING(data, 1, LEN(data)-3)) / 1024.0 / 1024.0 as 'Table Size (GB)' FROM #tbl", sourceConnection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var size = new TotalTableSize
                            {
                                TableName = reader.GetString(0),
                                TotalTableSizeGB = reader.GetDecimal(1)
                            };

                            tableSizes.Add(size);
                        }
                    }
                }

                using (var command = new SqlCommand("DROP TABLE #tbl", sourceConnection))
                {
                    command.ExecuteNonQuery();
                }

                var currentTableSize = tableSizes.FirstOrDefault(t => t.TableName == tableInfo.Name);

                if (currentTableSize != null)
                {
                    Log($"Table size information found for table {tableInfo.Name}.");
                    string tableName = tableInfo.Name;
                    string customerName = databaseName;

                    using (SqlCommand insertCommandCustomerInfo = new SqlCommand("INSERT INTO CustomerInfo (Date, CustomerName, TableName, totalTableSizeGB, TotalIndexSize) VALUES (@Date, @CustomerName, @TableName, @totalTableSizeGB, @TotalIndexSize)", destinationConnection))
                    {
                        insertCommandCustomerInfo.Parameters.AddWithValue("@Date", DateTime.Now);
                        insertCommandCustomerInfo.Parameters.AddWithValue("@CustomerName", customerName);
                        insertCommandCustomerInfo.Parameters.AddWithValue("@TableName", tableName);
                        insertCommandCustomerInfo.Parameters.AddWithValue("@totalTableSizeGB", currentTableSize.TotalTableSizeGB);

                        indexSizes = GetIndexSizes(sourceConnection); // indexSizes değişkenine değer ata

                        if (indexSizes.Any())
                        {
                            insertCommandCustomerInfo.Parameters.AddWithValue("@TotalIndexSize", indexSizes.Sum(i => i.IndexSizeGB));
                        }
                        else
                        {
                            insertCommandCustomerInfo.Parameters.AddWithValue("@TotalIndexSize", 0); // Varsayılan değer
                        }

                        // Diğer kolonlara varsayılan değerler atanıyor.
                        // insertCommandCustomerInfo.Parameters.AddWithValue("@FragmentationRatio", 0.0); // Varsayılan değer
                        // insertCommandCustomerInfo.Parameters.AddWithValue("@MostUsedIndex", DBNull.Value); // Varsayılan değer, NULL

                        insertCommandCustomerInfo.ExecuteNonQuery();
                    }

                    // TotalIndexSizeGB için IndexInfo tablosuna yazma işlemi
                    foreach (var indexSize in indexSizes)
                    {
                        using (SqlCommand insertCommandIndexInfo = new SqlCommand("INSERT INTO IndexInfo (Date, CustomerName, TableName, IndexName, IndexSizeGB) VALUES (@Date, @CustomerName, @TableName, @IndexName, @IndexSizeGB)", destinationConnection))
                        {
                            insertCommandIndexInfo.Parameters.AddWithValue("@Date", DateTime.Now);
                            insertCommandIndexInfo.Parameters.AddWithValue("@CustomerName", customerName);
                            insertCommandIndexInfo.Parameters.AddWithValue("@TableName", tableName);
                            insertCommandIndexInfo.Parameters.AddWithValue("@IndexName", indexSize.IndexName);
                            insertCommandIndexInfo.Parameters.AddWithValue("@IndexSizeGB", indexSize.IndexSizeGB);

                            insertCommandIndexInfo.ExecuteNonQuery();
                        }
                    }

                    Log($"Data sync for table {tableInfo.Name} completed using source connection: {sourceConnection.ConnectionString}. 1 row synchronized.");
                }
                else
                {
                    Log($"Table size information not found for table {tableInfo.Name}.");
                }
            }
            catch (Exception ex)
            {
                Log($"Error in SyncTableData for table {tableInfo.Name}: {ex.Message}");
            }
        }


        private List<IndexSize> GetIndexSizes(SqlConnection connection)
        {
            List<IndexSize> indexSizes = new List<IndexSize>();

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
                            IndexSizeGB = reader.GetDecimal(1),
                        };

                        indexSizes.Add(indexSize);
                    }
                }
            }

            using (SqlCommand dropTableCommand = new SqlCommand("DROP TABLE #tbl", connection))
            {
                dropTableCommand.ExecuteNonQuery();
            }

            return indexSizes;
        }
        private void Log(string message)
        {
            File.AppendAllText("D:\\MainGraphicsAPI\\CentralWriter\\logfile.txt", $"{DateTime.Now} - {message}\n");
        }

    }
}
