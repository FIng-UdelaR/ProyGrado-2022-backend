using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using static CloudManufacturingSharedLibrary.Constants;

namespace CloudManufacturingDBAccess.Migrations
{
    public class _2021051501InitialMigration : SQLMigration
    {
        public override string MigrationName { get => "2021051501InitialMigration"; }
        public override string Description { get => "Initial migration"; }

        public override bool Execute(ref SqlConnection cnn, List<string> migrationName)
        {
            if (migrationName != null && migrationName.Any(name => name == MigrationName)) //Migration ya ejecutada
                return true;
            if (cnn.State == ConnectionState.Closed)
                cnn.Open();
            bool result = false;

            try
            {
                MigrationHistory(ref cnn);
                CreateMachinesTable(ref cnn);

                MigrationExecuted(ref cnn); //Se deja constancia de la migración ejecutada
                result = true;
            }
            catch (Exception ex)
            {
                //Log.GetInstance().DoLog($"Error executing migration {MigrationName}", ex);
            }
            return result;
        }

        #region AUXILIAR METHODS
        private void MigrationHistory(ref SqlConnection cnn)
        {
            try
            {
                //Limpieza de variables
                columns = new Dictionary<string, KeyValuePair<SQLType, string>>();
                indexes = new Dictionary<string, KeyValuePair<SQLIndexType, string>>();
                constraints = new Dictionary<string, KeyValuePair<string, string>>();
                tableName = "__MIGRATION_HISTORY";
                primaryKey = "NAME";

                //Columnas
                columns.Add("NAME", ColumnData(SQLType.NVARCHAR, comment: "(255)", nullable: false));
                columns.Add("EXECUTION_DATE", ColumnData(SQLType.DATETIME2, nullable: false, defaultValue: "CONVERT(datetime, CONVERT(varchar, CURRENT_TIMESTAMP, 120), 121)"));

                //Índices
                indexes.Add("IDX_MIGRATION_NAME", IndexData(columnsAndOrder: "NAME DESC"));

                //FKs

                //Ejecución
                DBAccess.CreateTable(ref cnn, tableName, columns, indexes, constraints, primaryKey);
            }
            catch (Exception ex)
            {
                //Log.GetInstance().DoLog($"Exception executing method", ex);
            }
        }

        private void CreateMachinesTable(ref SqlConnection cnn)
        {
            try
            {
                //Limpieza de variables
                columns = new Dictionary<string, KeyValuePair<SQLType, string>>();
                indexes = new Dictionary<string, KeyValuePair<SQLIndexType, string>>();
                constraints = new Dictionary<string, KeyValuePair<string, string>>();
                tableName = "MACHINES";
                primaryKey = "ID";

                //Columnas
                columns.Add("ID", ColumnData(SQLType.INT, comment: "IDENTITY(1,1)", nullable: false));
                columns.Add("NAME", ColumnData(SQLType.NVARCHAR, comment: "(255)", nullable: false));
                columns.Add("DELETED", ColumnData(SQLType.BIT, nullable: false, defaultValue: "(0)"));
                columns.Add("PORT_NUMBER", ColumnData(SQLType.INT, nullable: false, defaultValue: "(0)"));
                columns.Add("URI", ColumnData(SQLType.NVARCHAR, comment: "(255)"));
                columns.Add("SUPPORTED_MATERIAL", ColumnData(SQLType.INT));
                columns.Add("SUPPORTED_SIZES", ColumnData(SQLType.NVARCHAR, comment: "(MAX)"));
                columns.Add("SUPPORTED_QUALITIES", ColumnData(SQLType.NVARCHAR, comment: "(MAX)"));
                columns.Add("LOCATION", ColumnData(SQLType.NVARCHAR, comment: "(MAX)"));

                //Índices

                //FKs

                //Ejecución
                DBAccess.CreateTable(ref cnn, tableName, columns, indexes, constraints, primaryKey);
            }
            catch (Exception ex)
            {
                //Log.GetInstance().DoLog($"Exception executing method", ex);
            }
        }
        #endregion AUXILIAR METHODS
    }
}
