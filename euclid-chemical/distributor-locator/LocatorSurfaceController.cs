using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using Euclid7.Models;
using System.Web.Mail;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI.WebControls;
using System.Web.UI;
using System.Text;
using umbraco.NodeFactory;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Xml.Linq;
using umbraco.presentation.nodeFactory;
using System.Data; 

namespace Euclid7.Controllers
{
    public class LocatorSurfaceController : SurfaceController
    {
        private static string connStr = ConfigurationManager.ConnectionStrings["euclidOASConn"].ConnectionString;
        private static SqlConnection conn = new SqlConnection(connStr);
        public umbraco.NodeFactory.Node CurrentPage = umbraco.NodeFactory.Node.GetCurrent(); 

		[ActionName("LoadDistributor")]
        public ActionResult LoadDistributor()
        {   
            DistributorParameters dp = new DistributorParameters();
            conn.Open();
            string selectString = "Select distinct state from [euclidOAS].[dbo].[distributor] where type = 1";
            SqlCommand cmd = new SqlCommand(selectString, conn);
            SqlDataReader reader = cmd.ExecuteReader();

			try
            {
                if (reader != null && reader.HasRows)
                {
                    dp.States = new List<SelectListItem>();                 
                    while (reader.Read())
                    {
                        dp.States.Add(new SelectListItem()
                        {
                            Text = reader[0].ToString().Replace(' ', '-'),
                            Value = reader[0].ToString()
                    });
                    }
                }
            }
            catch (Exception eo)
            {
                throw eo;
            }
            reader.Close();
            conn.Close();
            return PartialView("DistributorLocator", dp);
        }


		[ActionName("LoadSalesRep")]
		public ActionResult LoadSalesRep()
		{
			DistributorParameters dp = new DistributorParameters();
			conn.Open();
			string selectString = "Select distinct state from [euclidOAS].[dbo].[distributor] where type = 0";
			SqlCommand cmd = new SqlCommand(selectString, conn);
			SqlDataReader reader = cmd.ExecuteReader();
			try
			{
				if (reader != null && reader.HasRows)
				{
					dp.States = new List<SelectListItem>();
					while (reader.Read())
					{
						dp.States.Add(new SelectListItem()
						{
							Text = reader[0].ToString().Replace(' ', '-'),
							Value = reader[0].ToString()
						});
					}
				}
			}
			catch (Exception eo)
			{
				throw eo;
			}
			reader.Close();
			conn.Close();
			return PartialView("SalesRepLocator", dp);
		}


		[ActionName("GetDistributorResults")]
        public ActionResult GetDistributorResults(DistributorParameters dp, FormCollection form)
        {
			int stateZoom = Convert.ToInt32(CurrentPage.GetProperty("stateSearchMapZoom").ToString());
			int zipZoom = Convert.ToInt32(CurrentPage.GetProperty("zipSearchMapZoom").ToString());
			Session["distributors"] = null;
            string whereClause = "WHERE";
            string resultsFor = "";
            bool isZipSearch = false;
            if (string.IsNullOrWhiteSpace(dp.City) && string.IsNullOrWhiteSpace(dp.State) && string.IsNullOrWhiteSpace(dp.ZipCode))
            {
                //Display Error when no selections have been made
                TempData["error"] = CurrentPage.GetProperty("requiredValidationText") != null ? CurrentPage.GetProperty("requiredValidationText").ToString() : "Please indicate State or Zip.";
            }
            else if (!string.IsNullOrWhiteSpace(dp.ZipCode))
            {
                //Zip takes priority over City/State
                isZipSearch = true;
                string selectedZip = dp.ZipCode;                
                if (ModelState.IsValid)
                {
                    whereClause = whereClause + " a.postal_code = '" + selectedZip + "'";
                    resultsFor = "'" + selectedZip + "'"; 
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(dp.City) && string.IsNullOrWhiteSpace(dp.State))
                {
                    //search by City only if no State is selected
                    string selectedCity = dp.City;
                    whereClause = whereClause + " a.city LIKE '%" + selectedCity + "%'";
                    resultsFor = "'" + selectedCity.ToUpper() + "'";
                }
                else if (string.IsNullOrWhiteSpace(dp.City) && !string.IsNullOrWhiteSpace(dp.State))
                {
                    //search by State only if no City is selected
                    string selectedState = dp.State;
                    whereClause = whereClause + " a.state = '" + selectedState + "'";
                    resultsFor = "'" + selectedState.ToUpper() + "'";                    
                }
                else
                {
                    //search by State and City
                    string selectedCity = dp.City;
                    string selectedState = dp.State;                    
                    whereClause = whereClause + " a.city LIKE '%" + selectedCity + "%' AND a.state = '" + selectedState + "'";
                    resultsFor = "'" + selectedCity.ToUpper() + ", " + selectedState.ToUpper() + "'";
                }
            }

            if (whereClause != "WHERE")
            {                
                TempData["resultsFor"] = (CurrentPage.GetProperty("resultsForTextOverride") != null ? CurrentPage.GetProperty("resultsForTextOverride").ToString() : "RESULTS FOR") + " " + resultsFor;
                TempData["resultsFlag"] = true;

                DistributorResultSet drs = new DistributorResultSet();

                if (!isZipSearch)
                {
					TempData["mapZoom"] = stateZoom;
					DataSet ds = new DataSet();
                    List<Distributor> distributors = new List<Distributor>();
                    using (SqlConnection con = new SqlConnection(connStr))
                    {
                        string sql = "SELECT a.[name],a.[address1],a.[city],a.[state],a.[postal_code],a.[phone],a.[fax],a.[website], b.lat, b.long FROM  [dbo].[distributor] a LEFT JOIN  [dbo].[zips] b on a.postal_code = b.zip "  + whereClause + " AND[type] = 1   ORDER BY a.[name]";
                        using (SqlCommand sqlcmd = new SqlCommand(sql))
                        {
                            sqlcmd.Connection = con;
                            using (SqlDataAdapter sda = new SqlDataAdapter(sqlcmd))
                            {
                                sda.Fill(ds);
                            }
                        }
                    }
					int markerId = 1;
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        Distributor distributor = new Distributor();
                        distributor.Name = Convert.ToString(row["name"]);
                        distributor.Address = Convert.ToString(row["address1"]);
                        distributor.City = Convert.ToString(row["city"]).Replace(' ', '-');
                        distributor.StateValue = Convert.ToString(row["state"]).Replace(' ', '-');
                        distributor.State = Convert.ToString(row["state"]);
                        distributor.Zip = Convert.ToString(row["postal_code"]);
                        distributor.Phone = Convert.ToString(row["phone"]);
						distributor.Fax = Convert.ToString(row["fax"]);
						distributor.Website = Convert.ToString(row["website"]);
						if (row["lat"] != DBNull.Value && row["long"] != DBNull.Value)
						{
							distributor.Lat = Convert.ToString(row["lat"]);
							distributor.Long = "-" + Convert.ToString(row["long"]);
							distributor.MarkerId = markerId;
							markerId++;
						}
						else
						{
							distributor.Lat = "";
							distributor.Long = "";
							distributor.MarkerId = 0;
						}  
                        distributor.Distance = "undefined";						
						distributors.Add(distributor);		
                    }



					foreach (Distributor dis in distributors)
					{
						if (dis.Website.Length > 4 && !(dis.Website.Substring(0, 4) == "http"))
						{
							dis.Website = "http://" + dis.Website;
						}
					}



					//foreach (Distributor dis in distributors)
					//{
					//	using (WebClient wc = new WebClient())
					//	{
					//		var json = wc.DownloadString("https://maps.googleapis.com/maps/api/geocode/json?address=" + dis.Address + " " + dis.City + ", " + dis.State + " " + dis.Zip + "&key=AIzaSyCShc6ISfXBfzjitikfWY6pJIaQpw4dpxs");
					//		GoogleMapJSON googleMapJSON = JsonConvert.DeserializeObject<GoogleMapJSON>(json);
					//		dis.Lat = googleMapJSON.results[0].geometry.location.lat;
					//		dis.Long = googleMapJSON.results[0].geometry.location.lng;
					//	}
					//}
                    drs.Distributors = distributors;
                }
                else
                {
					TempData["mapZoom"] = zipZoom;
					TempData["resultsFor"] = (CurrentPage.GetProperty("resultsForTextOverride") != null ? CurrentPage.GetProperty("resultsForTextOverride").ToString() : "RESULTS FOR") + " " + resultsFor;
                    TempData["resultsFlag"] = true;                    
                    drs.Distributors = GetDistributorsByZip(dp.ZipCode, 50);
                }

                if (drs.Distributors.Count > 0)
                {
                    Session["distributors"] = drs;
                }
                else
                {
                    TempData["resultsFor"] = (CurrentPage.GetProperty("noResultsForTextOverride") != null ? CurrentPage.GetProperty("noResultsForTextOverride").ToString() : "NO RESULTS FOR") + " " + resultsFor;
                }
            }
			return CurrentUmbracoPage();
        }

		[ActionName("GetSalesRepResults")]
		public ActionResult GetSalesRepResults(DistributorParameters dp, FormCollection form)
		{
			Session["distributors"] = null;
			string whereClause = "WHERE";
			string resultsFor = "";
			bool isZipSearch = false;
			if (string.IsNullOrWhiteSpace(dp.City) && string.IsNullOrWhiteSpace(dp.State) && string.IsNullOrWhiteSpace(dp.ZipCode))
			{
				//Display Error when no selections have been made
				TempData["error"] = CurrentPage.GetProperty("requiredValidationText") != null ? CurrentPage.GetProperty("requiredValidationText").ToString() : "Please indicate State";
			}
			else if (!string.IsNullOrWhiteSpace(dp.ZipCode))
			{
				//Zip takes priority over City/State
				isZipSearch = true;
				string selectedZip = dp.ZipCode;
				if (ModelState.IsValid)
				{
					whereClause = whereClause + " a.postal_code = '" + selectedZip + "'";
					resultsFor = "'" + selectedZip + "'";
				}
			}
			else
			{
				if (!string.IsNullOrWhiteSpace(dp.City) && string.IsNullOrWhiteSpace(dp.State))
				{
					//search by City only if no State is selected
					string selectedCity = dp.City;
					whereClause = whereClause + " a.city LIKE '%" + selectedCity + "%'";
					resultsFor = "'" + selectedCity.ToUpper() + "'";
				}
				else if (string.IsNullOrWhiteSpace(dp.City) && !string.IsNullOrWhiteSpace(dp.State))
				{
					//search by State only if no City is selected
					string selectedState = dp.State;
					whereClause = whereClause + " a.state = '" + selectedState + "'";
					resultsFor = "'" + selectedState.ToUpper() + "'";
				}
				else
				{
					//search by State and City
					string selectedCity = dp.City;
					string selectedState = dp.State;
					whereClause = whereClause + " a.city LIKE '%" + selectedCity + "%' AND a.state = '" + selectedState + "'";
					resultsFor = "'" + selectedCity.ToUpper() + ", " + selectedState.ToUpper() + "'";
				}
			}

			if (whereClause != "WHERE")
			{
				TempData["resultsFor"] = (CurrentPage.GetProperty("resultsForTextOverride") != null ? CurrentPage.GetProperty("resultsForTextOverride").ToString() : "RESULTS FOR") + " " + resultsFor;
				TempData["resultsFlag"] = true;

				DistributorResultSet drs = new DistributorResultSet();

				if (!isZipSearch)
				{ 
					DataSet ds = new DataSet();
					List<Distributor> distributors = new List<Distributor>();
					using (SqlConnection con = new SqlConnection(connStr))
					{
						string sql = "SELECT a.[name],a.[address1],a.[city],a.[state],a.[postal_code],a.[email],a.[phone],a.[fax],a.[website] FROM[euclidOAS].[dbo].[distributor] a " + whereClause + "AND [type] = 0";
						using (SqlCommand sqlcmd = new SqlCommand(sql))
						{
							sqlcmd.Connection = con;
							using (SqlDataAdapter sda = new SqlDataAdapter(sqlcmd))
							{
								sda.Fill(ds);
							}
						}
					}

					foreach (DataRow row in ds.Tables[0].Rows)
					{
						Distributor distributor = new Distributor();
						distributor.Name = Convert.ToString(row["name"]);
						distributor.Address = Convert.ToString(row["address1"]);
						distributor.City = Convert.ToString(row["city"]).Replace(' ', '-');
						distributor.StateValue = Convert.ToString(row["state"]).Replace(' ', '-');
						distributor.State = Convert.ToString(row["state"]);
						distributor.Zip = Convert.ToString(row["postal_code"]);
						distributor.Email = Convert.ToString(row["email"]);
						distributor.Phone = Convert.ToString(row["phone"]);
						distributor.Fax = Convert.ToString(row["fax"]);
						distributor.Website = Convert.ToString(row["website"]); 
						distributor.Distance = "undefined";
						distributors.Add(distributor);
					}
					drs.Distributors = distributors;
				}
				else
				{
					TempData["resultsFor"] = (CurrentPage.GetProperty("resultsForTextOverride") != null ? CurrentPage.GetProperty("resultsForTextOverride").ToString() : "RESULTS FOR") + " " + resultsFor;
					TempData["resultsFlag"] = true;
					drs.Distributors = GetDistributorsByZip(dp.ZipCode, 25);
				}

				if (drs.Distributors.Count > 0)
				{
					Session["distributors"] = drs;
				}
				else
				{
					TempData["resultsFor"] = (CurrentPage.GetProperty("noResultsForTextOverride") != null ? CurrentPage.GetProperty("noResultsForTextOverride").ToString() : "NO RESULTS FOR") + " " + resultsFor;
				}
			}
			return CurrentUmbracoPage();
		}


		public List<Distributor> GetDistributorsByZip(string zipcode, int radius)
        {
            DataSet ds = new DataSet(); 
            List<Distributor> distributors = new List<Distributor>(); 
            
            using (SqlConnection con = new SqlConnection(connStr))
            {
                string proc = "usp_distributor_zip_search";
                using (SqlCommand cmd = new SqlCommand(proc))
                {
                    cmd.Parameters.Add("@zip", SqlDbType.VarChar).Value = zipcode;
                    cmd.Parameters.Add("@iRadius", SqlDbType.Int).Value = radius;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Connection = con;
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        sda.Fill(ds);
                    }
                }
            }
			int markerId = 1;
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                Distributor distributor = new Distributor();
                distributor.Name = Convert.ToString(row["name"]);
                distributor.Address = Convert.ToString(row["address1"]);
                distributor.City = Convert.ToString(row["city"]).Replace(' ', '-');
                distributor.StateValue = Convert.ToString(row["state"]).Replace(' ', '-');
                distributor.State = Convert.ToString(row["state"]);
                distributor.Zip = Convert.ToString(row["postal_code"]);
                distributor.Phone = Convert.ToString(row["phone"]);
				distributor.Fax = Convert.ToString(row["fax"]);
				distributor.Website = Convert.ToString(row["website"]);
                distributor.Lat = Convert.ToString(row["lat"]);
                distributor.Long = "-" + Convert.ToString(row["long"]);
                distributor.Distance = Math.Round(Convert.ToDecimal(row["distance"]), 2).ToString();
				distributor.MarkerId = markerId;
                distributors.Add(distributor);
				markerId++;
            }
			foreach (Distributor dis in distributors)
			{
				if (dis.Website.Length > 4 && !(dis.Website.Substring(0, 4) == "http"))
				{
					dis.Website = "http://" + dis.Website;
				}
			} 
			return distributors;
        }
    }
}