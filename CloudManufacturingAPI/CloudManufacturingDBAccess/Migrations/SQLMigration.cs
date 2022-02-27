using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using static CloudManufacturingSharedLibrary.Constants;

namespace CloudManufacturingDBAccess.Migrations
{
    public abstract class SQLMigration
    {
        #region AUXILIAR VARIABLES
        public string tableName;
        public Dictionary<string, KeyValuePair<SQLType, string>> columns;
        public Dictionary<string, KeyValuePair<SQLIndexType, string>> indexes;
        public Dictionary<string, KeyValuePair<string, string>> constraints;
        public string primaryKey;
        #endregion AUXILIAR VARIABLES

        #region PROPERTIES
        public abstract string MigrationName { get; }
        public abstract string Description { get; }
        #endregion PROPERTIES

        #region METHODS
        public abstract bool Execute(ref SqlConnection cnn, List<string> alreadyExecutedMigrations);
        public KeyValuePair<SQLType, string> ColumnData(SQLType type, string comment = "", bool nullable = true, string defaultValue = "")
        {
            string auxiliar = comment;
            auxiliar += nullable ? " NULL " : " NOT NULL ";
            if (!string.IsNullOrWhiteSpace(defaultValue))
                auxiliar += $"DEFAULT {defaultValue}";
            return new KeyValuePair<SQLType, string>(type, auxiliar);
        }

        public KeyValuePair<SQLIndexType, string> IndexData(SQLIndexType type = SQLIndexType.NONCLUSTERED, string columnsAndOrder = "")
        {
            return new KeyValuePair<SQLIndexType, string>(type, columnsAndOrder);
        }

        public KeyValuePair<string, string> ConstraintData(string sourceColumn, string destinationColumn)
        {
            return new KeyValuePair<string, string>(sourceColumn, destinationColumn);
        }

        public void MigrationExecuted(ref SqlConnection cnn)
        {
            try
            {
                if (cnn.State == ConnectionState.Closed)
                    cnn.Open();
                using SqlCommand cmd = cnn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = $@"IF NOT EXISTS (SELECT TOP 1 0 FROM __MIGRATION_HISTORY WHERE NAME = @checkName)
INSERT INTO __MIGRATION_HISTORY (NAME) VALUES (@checkName)";
                cmd.Parameters.Add(DBAccess.CustomSQLParameter("@checkName", MigrationName));
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                //Log.GetInstance().DoLog($"Exception inserting migration history '{MigrationName}'", ex);
            }
        }
        #endregion METHODS
    }
}
