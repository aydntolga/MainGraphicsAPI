using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.ServiceProcess;
using System.Data.SqlClient;
using System.Timers;
using ApiService.Infos;

namespace ApiService
{
    public partial class Service1 : ServiceBase
    {
        private Timer timer;
        private IConfigurationRoot configuration;

        public Service2()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Konfigürasyon dosyasını yükleyin
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            configuration = builder.Build();

            timer = new Timer();
            timer.Elapsed += new ElapsedEventHandler(OnTimerElapsed);
            timer.Interval = 60 * 1000;
            timer.Enabled = true;
        }

        protected override void OnStop()
        {
            timer.Enabled = false;
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            SyncAllDatabases();
        }

        private void SyncAllDatabases()
        {
            
            using (SqlConnection centralConnection = new SqlConnection(configuration.GetConnectionString("CentralDatabase")))
            {
                centralConnection.Open();

               
                foreach (var databaseInfo in GetDatabaseInfo())
                {
                    SyncDatabaseData(databaseInfo, centralConnection);
                }

                centralConnection.Close();
            }
        }

        private void SyncDatabaseData(DatabaseInfo databaseInfo, SqlConnection destinationConnection)
        {
            using (SqlConnection sourceConnection = new SqlConnection(configuration.GetConnectionString(databaseInfo.Name)))
            {
                sourceConnection.Open();

                foreach (var tableInfo in databaseInfo.Tables)
                {
                    SyncTableData(databaseInfo.Name, tableInfo, sourceConnection, destinationConnection);
                }

                sourceConnection.Close();
            }
        }

        private void SyncTableData(string databaseName, TableInfo tableInfo, SqlConnection sourceConnection, SqlConnection destinationConnection)
        {
            using (SqlCommand sourceCommand = new SqlCommand($"SELECT * FROM {tableInfo.Name}", sourceConnection))
            using (SqlDataReader reader = sourceCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    // Veritabanından gelen verileri oku
                    string customerName = reader["CustomerName"].ToString();
                    int totalTableSize = Convert.ToInt32(reader["TotalTableSize"]);
                    int totalIndexSizes = Convert.ToInt32(reader["TotalIndexSizes"]);
                    float fragmentationRatio = Convert.ToSingle(reader["FragmentationRatio"]);
                    string mostUsedIndexes = reader[tableInfo.MostUsedIndexColumn].ToString(); 
                    
                    using (SqlCommand insertCommand = new SqlCommand("INSERT INTO CustomerInfo (CustomerName, TotalTableSize, TotalIndexSizes, FragmentationRatio, MostUsedIndexes) VALUES (@CustomerName, @TotalTableSize, @TotalIndexSizes, @FragmentationRatio, @MostUsedIndexes)", destinationConnection))
                    {
                        insertCommand.Parameters.AddWithValue("@CustomerName", $"{databaseName} - {tableInfo.Name} - {customerName}");
                        insertCommand.Parameters.AddWithValue("@TotalTableSize", totalTableSize);
                        insertCommand.Parameters.AddWithValue("@TotalIndexSizes", totalIndexSizes);
                        insertCommand.Parameters.AddWithValue("@FragmentationRatio", fragmentationRatio);
                        insertCommand.Parameters.AddWithValue("@MostUsedIndexes", mostUsedIndexes);

                        insertCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        private List<DatabaseInfo> GetDatabaseInfo()
        {
            
            List<DatabaseInfo> databaseInfos = new List<DatabaseInfo>
            {
                new DatabaseInfo
                {
                    Name = "Northwind",
                    Tables = new List<TableInfo>
                    {
                        new TableInfo
                        {
                            Name = "Personeller",
                            TotalTableSizeColumn = "totalTableSizeGB",
                            MostUsedIndexColumn = "mostUsedIndex"
                        },
                        new TableInfo
                        {
                            Name = "Satislar",
                            TotalTableSizeColumn = "totalTableSizeGB",
                            MostUsedIndexColumn = "mostUsedIndex"
                        },
                        new TableInfo
                        {
                            Name = "Kategoriler",
                            TotalTableSizeColumn = "totalTableSizeGB",
                            MostUsedIndexColumn = "mostUsedIndex"
                        },
                        new TableInfo
                        {
                            Name = "sysdiagrams",
                            TotalTableSizeColumn = "totalTableSizeGB",
                            MostUsedIndexColumn = "mostUsedIndex"
                        },
                        new TableInfo
                        {
                            Name = "SatisDetaylar",
                            TotalTableSizeColumn = "totalTableSizeGB",
                            MostUsedIndexColumn = "mostUsedIndex"
                        }
                        // Ek tablo bilgilerini ekleyin
                    }
                },
                new DatabaseInfo
                {
                    Name= "Gratis",
                    Tables = new List<TableInfo>
                    {
                        new TableInfo
                        {
                            Name = "Kategoriler",
                            TotalTableSizeColumn = "totalTableSizeGB",
                            MostUsedIndexColumn = "mostUsedIndex"
                        },

                       new TableInfo
                       {
                            Name = "KozmetikFirmalari",
                            TotalTableSizeColumn = "totalTableSizeGB",
                            MostUsedIndexColumn = "mostUsedIndex"
                        },
                       new TableInfo
                       {
                            Name = "Musteriler",
                            TotalTableSizeColumn = "totalTableSizeGB",
                            MostUsedIndexColumn = "mostUsedIndex"
                        },
                       new TableInfo
                       {
                            Name = "SiparisDetaylari",
                            TotalTableSizeColumn = "totalTableSizeGB",
                            MostUsedIndexColumn = "mostUsedIndex"
                        },
                       new TableInfo
                       {
                            Name = "Siparisler",
                            TotalTableSizeColumn = "totalTableSizeGB",
                            MostUsedIndexColumn = "mostUsedIndex"
                        },
                       new TableInfo
                       {
                            Name = "Urunler",
                            TotalTableSizeColumn = "totalTableSizeGB",
                            MostUsedIndexColumn = "mostUsedIndex"
                        },
                    }
                }
                // Ek veritabanı bilgilerini ekleyin
            };

            return databaseInfos;
        }
    }
}
