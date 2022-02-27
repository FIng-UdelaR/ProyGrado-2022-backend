using CloudManufacturingDBAccess.Models;
using static CloudManufacturingSharedLibrary.Constants;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace CloudManufacturingDBAccess.Classes
{
    public class Machine
    {
        #region READ
        internal List<MachineDBO> Get()
        {
            List<MachineDBO> result = new List<MachineDBO>();
            try
            {
                using (SqlConnection cnn = new SqlConnection(DBAccess.DBConnectionString))
                {
                    using (SqlCommand cmd = cnn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT ID, NAME, PORT_NUMBER, URI, SUPPORTED_MATERIAL, SUPPORTED_SIZES, SUPPORTED_QUALITIES, LOCATION FROM MACHINES WHERE DELETED = 0";
                        cmd.CommandType = CommandType.Text;
                        cnn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                MachineDBO item = new MachineDBO()
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                    PortNumber = reader.GetInt32(2),
                                    Uri = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                    SupportedMaterial = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                                    SupportedSizes = reader.IsDBNull(5) ? new List<int>() : Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>>(reader.GetString(5)),
                                    SupportedQualities = reader.IsDBNull(6) ? new List<int>() : Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>>(reader.GetString(6)),
                                    Location = reader.IsDBNull(7) ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<Location>(reader.GetString(7))
                                };
                                result.Add(item);
                            }
                        }
                    }
                }

            }
            catch (Exception ex) { }
            return result;
        }
        #endregion READ

        #region WRITE
        internal int Insert(MachineDBO item)
        {
            if (!item.ValidateInsert())
                return -1;
            int result = -1;
            try
            {
                using (SqlConnection cnn = new SqlConnection(DBAccess.DBConnectionString))
                {
                    using (SqlCommand cmd = cnn.CreateCommand())
                    {
                        cmd.CommandText = $@"IF NOT EXISTS (SELECT TOP 1 ID FROM MACHINES WHERE DELETED = 0 AND PORT_NUMBER = @checkPort)
BEGIN
    {OUTPUT_START}
    INSERT INTO MACHINES (NAME, DELETED, PORT_NUMBER, URI, SUPPORTED_MATERIAL, SUPPORTED_SIZES, SUPPORTED_QUALITIES, LOCATION)
    output inserted.ID into @outputResult 
    VALUES (@NAME, @DELETED, @PORT_NUMBER, @URI, @SUPPORTED_MATERIAL, @SUPPORTED_SIZES, @SUPPORTED_QUALITIES, @LOCATION)
    {OUTPUT_END}
END
ELSE BEGIN SELECT -1 END";
                        cmd.Parameters.Add(DBAccess.CustomSQLParameter("@checkPort", item.PortNumber));
                        cmd.Parameters.Add(DBAccess.CustomSQLParameter("@NAME", item.Name));
                        cmd.Parameters.Add(DBAccess.CustomSQLParameter("@DELETED", false));
                        cmd.Parameters.Add(DBAccess.CustomSQLParameter("@PORT_NUMBER", item.PortNumber));
                        cmd.Parameters.Add(DBAccess.CustomSQLParameter("@URI", item.Uri));
                        cmd.Parameters.Add(DBAccess.CustomSQLParameter("@SUPPORTED_MATERIAL", item.SupportedMaterial));
                        cmd.Parameters.Add(DBAccess.CustomSQLParameter("@SUPPORTED_SIZES", Newtonsoft.Json.JsonConvert.SerializeObject(item.SupportedSizes)));
                        cmd.Parameters.Add(DBAccess.CustomSQLParameter("@SUPPORTED_QUALITIES", Newtonsoft.Json.JsonConvert.SerializeObject(item.SupportedQualities)));
                        cmd.Parameters.Add(DBAccess.CustomSQLParameter("@LOCATION", Newtonsoft.Json.JsonConvert.SerializeObject(item.Location)));

                        cmd.CommandType = CommandType.Text;
                        cnn.Open();
                        result = (int)cmd.ExecuteScalar();
                        cnn.Close();
                    }
                }
            }
            catch (Exception ex) { }
            return result;
        }

        internal int Delete(int[] ids)
        {
            int result = -1;
            try
            {
                using (SqlConnection cnn = new SqlConnection(DBAccess.DBConnectionString))
                {
                    using (SqlCommand cmd = cnn.CreateCommand())
                    {
                        cmd.CommandText = ids != null && ids.Length > 0
                            ? $"DELETE MACHINES WHERE ID IN ({string.Join(",", ids)})"
                            : "DELETE MACHINES";
                        cmd.CommandType = CommandType.Text;
                        cnn.Open();
                        result = cmd.ExecuteNonQuery();
                        cnn.Close();
                    }
                }
            }
            catch (Exception ex) { }
            return result;
        }
        #endregion WRITE

        #region AUXILIAR

        #endregion AUXILIAR
    }
}
