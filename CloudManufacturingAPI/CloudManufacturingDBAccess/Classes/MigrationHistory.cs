using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace CloudManufacturingDBAccess.Classes
{
    public class MigrationHistory
    {
        internal List<string> GetAllMigrations(ref SqlConnection cnn)
        {
            List<string> result = new List<string>();
            try
            {
                using SqlCommand cmd = cnn.CreateCommand();
                cmd.CommandText = $@"IF NOT EXISTS (SELECT TOP 1 0 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '__MIGRATION_HISTORY')
BEGIN SELECT NULL END
ELSE
BEGIN SELECT NAME FROM __MIGRATION_HISTORY END";
                cmd.CommandType = CommandType.Text;
                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                        result.Add(reader.GetString(0));
                }
            }
            catch (Exception ex)
            {
                //Log.GetInstance().DoLog($"Error getting the last migration", ex);
            }
            return result;
        }
    }
}
