using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LekkerenTsjap.Entities
{
    internal class ArduinoData
    {
        public bool IsCooling { get; set; }
        public double CurrentTemp { get; set; }
        public double RequestedTemp { get; set; }
    }
}
