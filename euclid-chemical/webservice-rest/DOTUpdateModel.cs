using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Euclid7.Models
{
    public class DOTUpdateModel
    {
        public string updateType { get; set; }
        public List<StateData> data { get; set; }
    }

    public class StateData
    {
        public string state { get; set; }
        public string product { get; set; }
        public int id { get; set; }
    }
}