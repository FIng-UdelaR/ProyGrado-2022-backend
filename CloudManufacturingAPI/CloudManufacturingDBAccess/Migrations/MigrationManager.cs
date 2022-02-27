using CloudManufacturingDBAccess.Classes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace CloudManufacturingDBAccess.Migrations
{
    public class MigrationManager
    {
        static MigrationHistory MigrationHistoryTable = new MigrationHistory();

        private static bool CheckDBConnection()
        {
            bool result = false;
            try
            {
                using (SqlConnection cnn = new SqlConnection(DBAccess.DBConnectionString))
                {
                    try
                    {
                        cnn.Open();
                        result = true;
                        cnn.Close();
                    }
                    catch (Exception ex) { }
                }
            }
            catch (Exception ex) { }
            return result;
        }

        public static void CheckMigrations(out bool connectionStringOk, out bool migrationsOk)
        {
            connectionStringOk = migrationsOk = false;
            try
            {
                connectionStringOk = CheckDBConnection();
                if (connectionStringOk)
                {
                    List<string> alreadyExecutedMigrations = null;
                    SqlConnection cnn = new SqlConnection(DBAccess.DBConnectionString);
                    cnn.Open();
                    try
                    {
                        alreadyExecutedMigrations = MigrationHistoryTable.GetAllMigrations(ref cnn);
                    }
                    catch (Exception ex) { /*Log.GetInstance().DoLog("Error getting last migration's name", ex);*/ }

                    try
                    {
                        migrationsOk = Migrate(ref cnn, alreadyExecutedMigrations);
                    }
                    catch (Exception ex) { /*Log.GetInstance().DoLog("Error executing pending migrations ", ex);*/ }
                    if (cnn.State == ConnectionState.Open)
                        cnn.Close();
                }
            }
            catch (Exception ex)
            {
                //Log.GetInstance().DoLog("Error checking migrations", ex);
            }
        }

        private static bool Migrate(ref SqlConnection cnn, List<string> lastMigration)
        {
            bool result = true;
            result = result && new _2021051501InitialMigration().Execute(ref cnn, lastMigration);
            result = result && new _2021060701AddOrdersToDatabase().Execute(ref cnn, lastMigration);

            return result;
        }
    }
}
