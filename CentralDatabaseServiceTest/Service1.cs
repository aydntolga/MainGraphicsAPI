using CentralDatabaseServiceTest.Infos;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Timers;


namespace CentralDatabaseServiceDemo
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
            timer.Interval = 60 * 1000; // Dakikada bir
            timer.Enabled = true;

            Log("Service started.");
        }

        protected override void OnStop()
        {
            timer.Enabled = false;
            Log("Service stopped.");
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

                foreach (var databaseInfo in GetDatabaseInfo())
                {
                    SyncDatabaseData(databaseInfo, centralConnection);
                }

                centralConnection.Close();
            }

            Log("Sync completed.");
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
        private void SyncTableData(string databaseName, TableInfo tableInfo, SqlConnection sourceConnection, SqlConnection destinationConnection)
        {
            Log($"Syncing data for table: {tableInfo.Name} using source connection: {sourceConnection.ConnectionString}");

            try
            {
                var tableSizes = new List<TotalTableSize>();

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


                var indexSizes = GetIndexSizes(sourceConnection);

                // Find the matching table size entry for the current table
                var currentTableSize = tableSizes.FirstOrDefault(t => t.TableName == tableInfo.Name);

                if (currentTableSize != null)
                {
                    string tableName = tableInfo.Name; // Sadece tablo adı
                    string customerName = $"{databaseName}"; // Sadece müşteri adı

                    // TotalTableSizeGB için CustomerInfo tablosuna yazma işlemi
                    using (SqlCommand insertCommandCustomerInfo = new SqlCommand("INSERT INTO CustomerInfo (Date, CustomerName, TableName, totalTableSizeGB, TotalIndexSize) VALUES (@Date, @CustomerName, @TableName, @totalTableSizeGB, @TotalIndexSize)", destinationConnection))
                    {
                        insertCommandCustomerInfo.Parameters.AddWithValue("@Date", DateTime.Now);
                        insertCommandCustomerInfo.Parameters.AddWithValue("@CustomerName", customerName);
                        insertCommandCustomerInfo.Parameters.AddWithValue("@TableName", tableName);
                        insertCommandCustomerInfo.Parameters.AddWithValue("@totalTableSizeGB", currentTableSize.TotalTableSizeGB);

                        // İndeks boyutlarını ekle
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

        private List<DatabaseInfo> GetDatabaseInfo()
        {
            List<DatabaseInfo> databaseInfos = new List<DatabaseInfo>
            {
                new DatabaseInfo
                {
                    Name = "Northwind",
                    ConnectionString = configuration.GetConnectionString("Northwind"),
                    Tables = new List<TableInfo>
                    {
                        new TableInfo
                        {
                            Name = "Personeller",
                            TotalTableSizeColumn = "totalTableSizeGB",
                            TotalIndexSizeColumn = "totalIndexSizesGB",
                            MostUsedIndexColumn = "mostUsedIndex"
                        },
                        new TableInfo
                        {
                            Name = "Satislar",
                            TotalTableSizeColumn = "totalTableSizeGB",
                            TotalIndexSizeColumn = "totalIndexSizesGB",
                            MostUsedIndexColumn = "mostUsedIndex"
                        },
                        new TableInfo
                        {
                            Name = "Kategoriler",
                            TotalTableSizeColumn = "totalTableSizeGB",
                            TotalIndexSizeColumn = "totalIndexSizesGB",
                            MostUsedIndexColumn = "mostUsedIndex"
                        },
                        new TableInfo
                        {
                            Name = "sysdiagrams",
                            TotalTableSizeColumn = "totalTableSizeGB",
                            TotalIndexSizeColumn = "totalIndexSizesGB",
                            MostUsedIndexColumn = "mostUsedIndex"
                        },
                        new TableInfo
                        {
                            Name = "SatisDetaylar",
                            TotalTableSizeColumn = "totalTableSizeGB",
                            TotalIndexSizeColumn = "totalIndexSizesGB",
                            MostUsedIndexColumn = "mostUsedIndex"
                        }
                    }
                },
                new DatabaseInfo
                {
                    Name= "Gratis",
                    ConnectionString = configuration.GetConnectionString("Gratis"),
                    Tables = new List<TableInfo>
                    {
                        new TableInfo
                        {
                            Name = "Kategoriler",
                            TotalTableSizeColumn = "totalTableSizeGB",
                            TotalIndexSizeColumn = "totalIndexSizesGB",
                            MostUsedIndexColumn = "mostUsedIndex"
                        },
                        new TableInfo
                        {
                            Name = "KozmetikFirmalari",
                            TotalTableSizeColumn = "totalTableSizeGB",
                            TotalIndexSizeColumn = "totalIndexSizesGB",
                            MostUsedIndexColumn = "mostUsedIndex"
                        },
                        new TableInfo
                        {
                            Name = "Musteriler",
                            TotalTableSizeColumn = "totalTableSizeGB",
                            TotalIndexSizeColumn = "totalIndexSizesGB",
                            MostUsedIndexColumn = "mostUsedIndex"
                        },
                        new TableInfo
                        {
                            Name = "SiparisDetaylari",
                            TotalTableSizeColumn = "totalTableSizeGB",
                            TotalIndexSizeColumn = "totalIndexSizesGB",
                            MostUsedIndexColumn = "mostUsedIndex"
                        },
                        new TableInfo
                        {
                            Name = "Siparisler",
                            TotalTableSizeColumn = "totalTableSizeGB",
                            TotalIndexSizeColumn = "totalIndexSizesGB",
                            MostUsedIndexColumn = "mostUsedIndex"
                        },
                        new TableInfo
                        {
                            Name = "Urunler",
                            TotalTableSizeColumn = "totalTableSizeGB",
                            TotalIndexSizeColumn = "totalIndexSizesGB",
                            MostUsedIndexColumn = "mostUsedIndex"
                        }
                    }
                }
            };

            return databaseInfos;
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
        private List<FragmentationTable> GetHighFragmentationIndexes(string connectionString)
        {
            List<FragmentationTable> fragmentedIndexes = new List<FragmentationTable>();

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Bağlantı bilgilerinden database adını çıkart
                    string databaseName = connection.Database;

                    using (var command = new SqlCommand("SELECT name, avg_fragmentation_in_percent FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, NULL) AS ps JOIN sys.indexes AS i ON ps.[object_id] = i.[object_id] AND ps.index_id = i.index_id WHERE ps.avg_fragmentation_in_percent >= 30", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var fragmentedIndex = new FragmentationTable
                                {
                                    Date = DateTime.Now,
                                    CustomerName = databaseName,
                                    IndexName = reader.GetString(0),
                                    FragmentationRatio = Convert.ToDecimal(reader["avg_fragmentation_in_percent"])
                                };

                                fragmentedIndexes.Add(fragmentedIndex);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Hata durumunda loglama veya başka bir işlem yapılabilir
                Console.WriteLine($"Error in GetHighFragmentationIndexes: {ex.Message}");
            }

            return fragmentedIndexes;
        }


        private void Log(string message)
        {
            File.AppendAllText("D:\\MainGraphicsAPI\\CentralDatabaseServiceDemo\\logfile.txt", $"{DateTime.Now} - {message}\n");
        }
    }
}