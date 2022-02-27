using CloudManufacturingDBAccess.Classes;
using CloudManufacturingDBAccess.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using static CloudManufacturingSharedLibrary.Constants;

namespace CloudManufacturingDBAccess
{
    public class DBAccess
    {
        static Machine Machines;
        static Order Orders;
        public static string DBConnectionString;
        private static readonly DBAccess _instance = new DBAccess();

        private DBAccess() { }

        public static DBAccess GetInstance() => _instance;

        public void Initialize(string dbConnectionString)
        {
            Machines = new Machine();
            Orders = new Order();
            DBConnectionString = dbConnectionString;
            Migrations.MigrationManager.CheckMigrations(out bool connectionStringOk, out bool migrationsOk); //TODO: on error, log something.
        }

        #region MACHINE
        public List<MachineDBO> GetMachines()
        {
            return Machines.Get();
        }

        public int InsertMachine(MachineDBO item)
        {
            return Machines.Insert(item);
        }

        public int DeleteMachines(int[] ids = null)
        {
            return Machines.Delete(ids);
        }
        #endregion MACHINE

        #region ORDER
        public List<OrderDBO> GetOrders(out int filteredRecords, out int totalRecords, int start = 0, int length = 1, string order = "ID ASC", string orderId = null)
        {
            return Orders.Get(out filteredRecords, out totalRecords, start, length, order, orderId);
        }

        public string InsertOrder(OrderDBO item)
        {
            return Orders.Insert(item);
        }

        public void UpdateOrderMachine(string orderId, IEnumerable<int> machineIdsToRemove, IEnumerable<int> machineIdsToAdd)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return;

            if (machineIdsToAdd != null) machineIdsToAdd = machineIdsToAdd.Distinct().ToList();
            if (machineIdsToRemove != null) machineIdsToRemove = machineIdsToRemove.Distinct().ToList();

            if (machineIdsToAdd != null && machineIdsToRemove != null && machineIdsToAdd.Count() == machineIdsToRemove.Count())
            {
                for (int i = 0; i < machineIdsToAdd.Count(); i++)
                {
                    Orders.ReplaceMachine(orderId, machineIdsToRemove.ElementAt(i), machineIdsToAdd.ElementAt(i));
                }
            }
            else
            {
                if (machineIdsToAdd != null && machineIdsToAdd.Any())
                {
                    foreach (var machineId in machineIdsToAdd)
                    {
                        Orders.AddMachine(orderId, machineId);
                    }
                }

                if (machineIdsToRemove != null && machineIdsToRemove.Any())
                {
                    foreach (var machineId in machineIdsToRemove)
                    {
                        Orders.RemoveMachine(orderId, machineId);
                    }
                }
            }
        }

        public void UpdateOrderEstimatedCompletionDate(string orderId, DateTime newDatetime, bool onlyIfGreater = false)
        {
            Orders.UpdateEstimatedCompletionDate(orderId, newDatetime, onlyIfGreater);
        }
        #endregion ORDER

        #region AUXILIAR METHODS
        public static SqlParameter CustomSQLParameter(string parameterName, object value)
        {
            return value == null
                || (value is string @parameterValue && string.IsNullOrWhiteSpace(@parameterValue)) //Empty string
                || (value is byte[] @parameterByteArray && (@parameterByteArray == null || @parameterByteArray.Length == 0)) //Empty binary
                    ? new SqlParameter(parameterName, DBNull.Value)
                    : new SqlParameter(parameterName, value);
        }

        public static string DatatableFilter(int start, int length, string order)
        {
            string result = "";
            if (!string.IsNullOrWhiteSpace(order))
                result += $"ORDER BY {order} ";
            if (length > 0)
                result += $"OFFSET {start} ROWS FETCH NEXT {length} ROWS ONLY ";
            return result;
        }

        public static void CreateTable(ref SqlConnection cnn, string tableName, Dictionary<string, KeyValuePair<SQLType, string>> columns, Dictionary<string, KeyValuePair<SQLIndexType, string>> indexes,
            Dictionary<string, KeyValuePair<string, string>> constraints, string primaryKey)
        {

            string s = "";
            foreach (KeyValuePair<string, KeyValuePair<SQLType, string>> valuePair in columns)
            {
                s = s + valuePair.Key + " " + valuePair.Value.Key.ToString() + " " + valuePair.Value.Value + ",";
            }
            if (!string.IsNullOrEmpty(primaryKey))
                s = s + "CONSTRAINT PK_" + tableName + " PRIMARY KEY(" + primaryKey + ")";
            else
                s = s.TrimEnd(',');

            try
            {
                if (cnn.State == ConnectionState.Closed)
                    cnn.Open();
                #region TABLE
                using (SqlCommand cmd = cnn.CreateCommand())
                {
                    cmd.CommandText = $@"IF NOT EXISTS (SELECT TOP 1 0  FROM INFORMATION_SCHEMA.TABLES  WHERE TABLE_TYPE='BASE TABLE' AND TABLE_NAME='{tableName}') 
BEGIN
    CREATE TABLE {tableName} ({s})
END";
                    cmd.CommandType = CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
                #endregion TABLE

                #region INDEXES
                if (indexes != null)
                {
                    foreach (KeyValuePair<string, KeyValuePair<SQLIndexType, string>> pair in indexes)
                    {
                        TryAddIndexToTable(ref cnn, tableName, pair.Key, pair.Value.Key, pair.Value.Value);
                    }
                }
                #endregion INDEXES

                #region CONSTRAINTS
                if (constraints != null)
                {
                    foreach (KeyValuePair<string, KeyValuePair<string, string>> pair in constraints)
                    {
                        TryAddConstraintToTable(ref cnn, tableName, pair.Key, pair.Value.Key, pair.Value.Value);
                    }
                }
                #endregion CONSTRAINTS
            }
            catch (Exception ex)
            {
                //Log.GetInstance().DoLog($"Error creating table '{tableName}'", ex);
            }
        }

        public static void TryAddIndexToTable(ref SqlConnection cnn, string tableName, string indexName, SQLIndexType indexType, string fieldsList)
        {
            try
            {
                if (cnn.State == ConnectionState.Closed)
                    cnn.Open();
                using SqlCommand cmd = cnn.CreateCommand();
                cmd.CommandText = $@"IF NOT EXISTS (SELECT TOP 1 0 FROM sys.indexes WHERE name = '{indexName}' AND object_id = OBJECT_ID('{tableName}')) 
BEGIN 
CREATE {indexType} INDEX {indexName } ON {tableName } ( {fieldsList}) 
END";
                cmd.CommandType = CommandType.Text;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                //Log.GetInstance().DoLog($"Exception trying to add index {indexName} to table {tableName}", ex);
            }
        }

        public static void TryAddConstraintToTable(ref SqlConnection cnn, string sourceTable, string destinationTable, string sourceField, string destinationField)
        {
            string constraintName = $"FK_{sourceTable}_{sourceField}";
            try
            {
                if (cnn.State == ConnectionState.Closed)
                    cnn.Open();
                using SqlCommand cmd = cnn.CreateCommand();
                cmd.CommandText = $@"IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name ='{constraintName}') 
BEGIN 
ALTER TABLE {sourceTable} WITH CHECK ADD CONSTRAINT {constraintName} FOREIGN KEY({sourceField}) REFERENCES {destinationTable} ({destinationField}) 
END";
                cmd.CommandType = CommandType.Text;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                //Log.GetInstance().DoLog($"Exception trying to add constraint {constraintName} to table {sourceTable}", ex);
            }
        }

        public static void TryAddColumnToTable(ref SqlConnection cnn, string tableName, string columnName, string columnType, object defaultValue = null)
        {
            try
            {
                if (cnn.State == ConnectionState.Closed)
                    cnn.Open();
                using SqlCommand cmd = cnn.CreateCommand();
                cmd.CommandText = $@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}') ALTER TABLE {tableName} ADD {columnName} {columnType};";
                cmd.CommandType = CommandType.Text;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                //Log.GetInstance().DoLog($"Error trying to add column {columnName} to table {tableName}", ex);
            }

            if (defaultValue != null)
            {
                try
                {
                    using SqlCommand cmd = cnn.CreateCommand();
                    cmd.CommandText = $"UPDATE {tableName} SET {columnName} = @defaultValue WHERE {columnName} IS NULL;";
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Add(CustomSQLParameter("@defaultValue", defaultValue));
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    //Log.GetInstance().DoLog($"Error trying to add default value {defaultValue} to column {columnName} of table {tableName}", ex);
                }
            }
        }

        public static void TryDropTable(ref SqlConnection cnn, string tableName)
        {
            try
            {
                if (cnn.State == ConnectionState.Closed)
                    cnn.Open();

                using (SqlCommand cmd = cnn.CreateCommand())
                {
                    cmd.CommandText = $"DROP TABLE IF EXISTS {tableName};";
                    cmd.CommandType = CommandType.Text;
                    cmd.ExecuteScalar();
                }
                cnn.Close();
            }
            catch (Exception ex)
            {
                //Log.GetInstance().DoLog($"Exception dropping table '{tableName}'", ex);
            }
        }

        public static int CountRegistersByCondition(string tableName, string conditions)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return 0;
            int result = 0;
            try
            {
                using SqlConnection cnn = new SqlConnection(DBConnectionString);
                cnn.Open();

                using (SqlCommand cmd = cnn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT COUNT(0) FROM {tableName}";
                    if (!string.IsNullOrWhiteSpace(conditions)) cmd.CommandText += $" WHERE {conditions}";
                    cmd.CommandType = CommandType.Text;
                    result = (int)cmd.ExecuteScalar();
                }
                cnn.Close();
            }
            catch (Exception ex)
            {
                //Log.GetInstance().DoLog($"Exception counting elements from table '{tableName}' with condition='{conditions}'", ex);
            }
            return result;
        }
        #endregion AUXILIAR METHODS
    }
}
