using System;

namespace CentralDatabaseServiceDemo
{
    internal class FragmentationTable
    {
        public DateTime Date { get; set; }
        public string CustomerName { get; set; }
        public string IndexName { get; set; }
        public decimal FragmentationRatio { get; set; }
    }
}