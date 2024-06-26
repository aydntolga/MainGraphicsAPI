﻿using MainGraphicsAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace MainGraphicsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IndexController : Controller
    {
        private readonly MyDbContext _context;
        private readonly IConfiguration _configuration;

        public IndexController(MyDbContext context, IConfiguration configuration)
        {
            _context = context;
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
        public async Task<IActionResult> GetFragmentedIndexes()
        {
            var fragmentedIndexes = new List<FragmentedIndex>();

            try
            {
                using (var connection = new SqlConnection(GetDatabaseConnectionString()))
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
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }
    }
}
