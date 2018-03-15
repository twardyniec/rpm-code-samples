using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Euclid7.Models
{
    public class DistributorResultSet
    {
        public List<Distributor> Distributors { get; set; } 
    }

    public class DistributorParameters
    {        
        public string City { get; set; }
        
        public string State { get; set; }

        public List<SelectListItem> States { get; set; }
        

        [RegularExpression("^[0-9]*$", ErrorMessage = " Zip must be a number")]
        public string ZipCode { get; set; }
    }

        public class Distributor
    {
        public string Name { get; set; }

        public string Address { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string StateValue { get; set; }

        public string Zip { get; set; }

		public string Email { get; set; }

		public string Phone { get; set; }

		public string Fax { get; set; }

		public string Website { get; set; }

        public string Lat { get; set; }

        public string Long { get; set; }

        public string Distance { get; set; }
		public int MarkerId { get; set; }
	}
}