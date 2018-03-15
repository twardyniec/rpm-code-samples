using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Euclid7.Models;
using Newtonsoft.Json;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Web.Script.Serialization;

namespace Euclid7.Services
{
	// NOTE: In order to launch WCF Test Client for testing this service, please select DotUpdate.svc or DotUpdate.svc.cs at the Solution Explorer and start debugging.
	public class DotUpdate : IDotUpdate
    {
		private static string connString = ConfigurationManager.ConnectionStrings["Euclid7Conn"].ConnectionString;
		string errorMsg = "Invalid updateType";
		string successMsg = "";
		
		public string Update(Stream JSONdataStream)
        {
			JavaScriptSerializer jss = new JavaScriptSerializer();
			try
			{
				StreamReader reader = new StreamReader(JSONdataStream);
				string jsonData = reader.ReadToEnd(); 
				DOTUpdateModel dotUpdate = JsonConvert.DeserializeObject<DOTUpdateModel>(jsonData);

				//check for nulls before continuing
				if (dotUpdate.updateType == "add" || dotUpdate.updateType == "delete" || dotUpdate.updateType == "reset")
				{
					foreach (StateData state in dotUpdate.data)
					{
						if (state.id == 0 || state.product == null || state.state == null)
						{
							return "Error: id, product, and state are required for " + dotUpdate.updateType;
						}
					}
				}
				else
				{
					foreach (StateData state in dotUpdate.data)
					{
						if (state.id == 0 || state.product == null)
						{
							return "Error: id and product are required for " + dotUpdate.updateType;
						}
					}
				}

				bool success = false;
				switch (dotUpdate.updateType)
				{
					case "reset":
						success = Reset(dotUpdate.data);
						break;
					case "update":
						success = Update(dotUpdate.data);
						break;
					case "add":
						success = Add(dotUpdate.data);
						break;
					case "delete":
						success = Delete(dotUpdate.data);
						break;
				}

				if (!success)
					return errorMsg;
				else
					return "Success: " + successMsg;
			}
			catch (Exception err)
			{
				return err.Message;
			}
		}

		private bool Reset(List<StateData> stateData)
		{
			int totalRows = 0;
			using (SqlConnection conn = new SqlConnection(connString))
			{
				try
				{
					conn.Open();
					using (SqlTransaction trans = conn.BeginTransaction())
					{
						using (SqlCommand command = new SqlCommand("", conn, trans))
						{
							command.CommandType = System.Data.CommandType.Text;

							//delete all records
							try
							{
								command.CommandText = "DELETE FROM [Euclid7].[dbo].[dotapprovals]";
								command.ExecuteNonQuery();
							}
							catch (Exception err)
							{
								trans.Rollback();
								if (conn.State == System.Data.ConnectionState.Open)
									conn.Close();
								errorMsg = string.Format("An error occurred: {0}", err.Message);
								return false;
							}

						}
						foreach (StateData state in stateData)
						{
							using (SqlCommand command = new SqlCommand("", conn, trans))
							{
								//check if record already exists						 
								command.CommandText = "SELECT COUNT(*) FROM [Euclid7].[dbo].[dotapprovals] WHERE [state] = @state AND [product] = @product and [productID] = @productID";
								command.Parameters.AddWithValue("@state", state.state);
								command.Parameters.AddWithValue("@product", state.product);
								command.Parameters.AddWithValue("@productID", state.id);
								Int32 rowCount = (Int32)command.ExecuteScalar();

								if (rowCount == 0)
								{
									try
									{
										command.CommandText = "INSERT INTO [Euclid7].[dbo].[dotapprovals] ([state],[product],[productID]) VALUES (@state, @product, @productID)";
										totalRows += command.ExecuteNonQuery();
									}
									catch (Exception err)
									{
										trans.Rollback();
										if (conn.State == System.Data.ConnectionState.Open)
											conn.Close();
										errorMsg = string.Format("An error occurred: {0}", err.Message);
										return false;
									}
								}
							}
						}						
						trans.Commit();
						if (conn.State == System.Data.ConnectionState.Open)
							conn.Close();
						successMsg = Convert.ToString(totalRows) + " row(s) inserted";
						return true;
					}
				}
				catch (Exception err) 
				{
					if (conn.State == System.Data.ConnectionState.Open)
						conn.Close();
					errorMsg = string.Format("An error occurred: {0}", err.Message);
					return false;
				}
			}
		}

		//update: Update product name (based on prodId)
		private bool Update(List<StateData> stateData)
		{
			SqlConnection conn = new SqlConnection(connString);
			conn.Open();
			int totalRows = 0;
			List<int> updatedProducts = new List<int>();
			try
			{
				foreach (StateData state in stateData)
				{
					using (SqlCommand command = new SqlCommand())
					{
						if (!updatedProducts.Contains(state.id))
						{
							command.Connection = conn;
							command.CommandText = "UPDATE [Euclid7].[dbo].[dotapprovals] SET [product] = @product WHERE [productID] = @productID";
							command.Parameters.AddWithValue("@product", state.product);
							command.Parameters.AddWithValue("@productID", state.id);
							totalRows += command.ExecuteNonQuery();
							updatedProducts.Add(state.id);
						}
					}
				}
				successMsg = Convert.ToString(totalRows) + " row(s) updated";
				conn.Close();
				return true;
			}
			catch (Exception err)
			{
				errorMsg = string.Format("An error occurred: {0}", err.Message);
				conn.Close();
				return false;
			}
		}

		//add: add new product to table
		private bool Add(List<StateData> stateData)
        {
			SqlConnection conn = new SqlConnection(connString);
			conn.Open();
			int totalRows = 0;
			try
			{
				//insert new records
				foreach (StateData state in stateData)
				{	
					using (SqlCommand command = new SqlCommand())
					{
						command.Connection = conn;

						//check if record already exists						 
						command.CommandText = "SELECT COUNT(*) FROM [Euclid7].[dbo].[dotapprovals] WHERE [state] = @state AND [product] = @product and [productID] = @productID";
						command.Parameters.AddWithValue("@state", state.state);
						command.Parameters.AddWithValue("@product", state.product);
						command.Parameters.AddWithValue("@productID", state.id);
						Int32 rowCount = (Int32)command.ExecuteScalar();

						if (rowCount == 0)
						{
							command.CommandText = "INSERT INTO [Euclid7].[dbo].[dotapprovals] ([state],[product],[productID]) VALUES (@state, @product, @productID)";
							totalRows += command.ExecuteNonQuery();
						}					
					}
				}
				successMsg = Convert.ToString(totalRows) + " row(s) added";
				conn.Close();
				return true;
			}
			catch (Exception err)
			{
				errorMsg = string.Format("An error occurred: {0}", err.Message);
				conn.Close();
				return false;
			}
		}

        //delete: remove product from table (state, prodName, Id req)
        private bool Delete(List<StateData> stateData)
        {
			SqlConnection conn = new SqlConnection(connString);
			conn.Open();
			int totalRows = 0;
			try
			{
				foreach (StateData state in stateData)
				{  
					using (SqlCommand command = new SqlCommand())
					{
						command.Connection = conn;
						command.CommandText = "DELETE FROM [Euclid7].[dbo].[dotapprovals] WHERE state = @state and product = @product and productID = @productID";
						command.Parameters.AddWithValue("@state", state.state);
						command.Parameters.AddWithValue("@product", state.product);
						command.Parameters.AddWithValue("@productID", state.id);
						totalRows += command.ExecuteNonQuery();						
					} 
				}
				successMsg = Convert.ToString(totalRows) + " row(s) deleted";
				conn.Close();
				return true;
			}
			catch (Exception err)
			{
				errorMsg = string.Format("An error occurred: {0}", err.Message);
				conn.Close();
				return false;
			}
        }

    }
}