using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mileage581
{
    public class Mileage
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }
        public int VehicleId { get; set; }
        public float Value { get; set; }
    }
}
