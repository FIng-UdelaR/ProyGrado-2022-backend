using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using static CloudManufacturingSharedLibrary.Constants;

namespace CloudManufacturingDBAccess.Migrations
{
    class _2021060701AddOrdersToDatabase : SQLMigration
    {
        public override string MigrationName { get => "2021060701AddOrdersToDatabase"; }
        public override string Description { get => "Add Orders To Database"; }

        public override bool Execute(ref SqlConnection cnn, List<string> migrationName)
        {
            if (migrationName != null && migrationName.Any(name => name == MigrationName)) //Migration ya ejecutada
                return true;
            if (cnn.State == ConnectionState.Closed)
                cnn.Open();
            bool result = false;

            try
            {
                CreateOrdersTable(ref cnn);

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
        private void CreateOrdersTable(ref SqlConnection cnn)
        {
            try
            {
                //Limpieza de variables
                columns = new Dictionary<string, KeyValuePair<SQLType, string>>();
                indexes = new Dictionary<string, KeyValuePair<SQLIndexType, string>>();
                constraints = new Dictionary<string, KeyValuePair<string, string>>();
                tableName = "ORDERS";
                primaryKey = "ID";

                //Columnas
                columns.Add("ID", ColumnData(SQLType.NVARCHAR, comment: "(50)", nullable: false));
                columns.Add("ARRIVAL_DATE", ColumnData(SQLType.DATETIME2, nullable: false));
                columns.Add("ESTIMATED_COMPLETION_DATE", ColumnData(SQLType.DATETIME2, nullable: false));
                columns.Add("WORKLOADS", ColumnData(SQLType.NVARCHAR, comment: "(MAX)"));
                columns.Add("MACHINE_IDS", ColumnData(SQLType.NVARCHAR, comment: "(MAX)"));

                //Índices
                //indexes.Add("IDX_ORDER_ID", IndexData(columnsAndOrder: "ID ASC"));

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
