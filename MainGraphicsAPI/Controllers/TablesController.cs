﻿using MainGraphicsAPI.Models;
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
        private readonly ITableService _tableService;

        public TablesController(MyDbContext context, ITableService tableService)
        {
            _context = context;
            _tableService=tableService;
        }

        [HttpGet("table")]
        public ActionResult<IEnumerable<TotalTableSize>> GetTableSizes()
        {
            var tableSizes = _tableService.GetTableSizes();
            return Ok(tableSizes);
        }


        [HttpGet("GetIndexSizes")]
        public ActionResult<IEnumerable<IndexSize>> GetIndexSizes()
        {
            var indexSizes = _tableService.GetIndexSizes();
            return Ok(indexSizes);
        }
    }
}
