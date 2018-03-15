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
    public class DotApprovalsSurfaceController : SurfaceController
    {
		private static string connStr = ConfigurationManager.ConnectionStrings["Euclid7Conn"].ConnectionString;
		private static SqlConnection conn = new SqlConnection(connStr);
		[ActionName("LoadDotApprovals")]
        public ActionResult LoadDotApprovals()
        {
			DotApprovals dot = new DotApprovals();
            conn.Open();
            string selectString = "Select distinct b.[State_Name], a.state from [Euclid7].[dbo].[dotapprovals] a join [dbo].[States] b on a.state = b.State_Code order by a.state ";
            SqlCommand cmd = new SqlCommand(selectString, conn);
            SqlDataReader reader = cmd.ExecuteReader();
            try
            {
                if (reader != null && reader.HasRows)
                {
					dot.States = new List<SelectListItem>();                 
                    while (reader.Read())
                    {
						dot.States.Add(new SelectListItem()
                        {
                            Text = reader[0].ToString(),
                            Value = reader[1].ToString().Replace(' ', '-')
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
			
			return PartialView("DotApprovals", dot);
        }

		[ActionName("GetDotApprovals")]
		public ActionResult GetDotApprovals(string state_code)
		{
			DotApprovals approvals = new DotApprovals();


			DataSet ds = new DataSet();
			List<string> products = new List<string>();
			using (SqlConnection con = new SqlConnection(connStr))
			{
				string sql = "select [product] from [Euclid7].[dbo].[dotapprovals] where [state] = '" + state_code + "' ORDER BY [product]";
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
				products.Add(Convert.ToString(row["product"]));
			}
			approvals.Products = products;

			ds.Clear();
			try
			{
				using (SqlConnection con = new SqlConnection(connStr))
				{
					string sql = "select top 1 [State_Name] from [Euclid7].[dbo].[States] where [State_Code] = '" + state_code + "'";
					using (SqlCommand sqlcmd = new SqlCommand(sql))
					{
						sqlcmd.Connection = con;
						using (SqlDataAdapter sda = new SqlDataAdapter(sqlcmd))
						{
							sda.Fill(ds);
						}
					}
				}
				approvals.StateName = Convert.ToString(ds.Tables[0].Rows[0]["State_Name"]);
			}
			catch
			{
				approvals.StateName = state_code;
			}
			return PartialView("DotApprovalsResults", approvals);
		}
	}
}