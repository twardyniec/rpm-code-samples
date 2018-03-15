using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Euclid7.Models
{
    public class DotApprovals
	{ 
        public List<string> Products { get; set; }      

		public string State { get; set; }
		public string StateName { get; set; }

		public List<SelectListItem> States { get; set; }

		 
    }
}