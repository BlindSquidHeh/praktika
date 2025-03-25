using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Практика_по_архиву.Classes
{
    public class ProductView
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public int? ProductTypeID { get; set; }
        public string ProductTypeTitle { get; set; }
        public string ArticleNumber { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public int? ProductionPersonCount { get; set; }
        public int? ProductionWorkshopNumber { get; set; }
        public decimal MinCostForAgent { get; set; }
        public string Materials { get; set; }
        public bool IsNotSoldLastMonth { get; set; }
    }
}
