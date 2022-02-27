using CloudManufacturingDBAccess.Models;
using static CloudManufacturingSharedLibrary.Constants;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using CloudManufacturingSharedLibrary.Models;

namespace CloudManufacturingDBAccess.Classes
{
    public class Order
    {
        internal List<OrderDBO> Get(out int filteredRecords, out int totalRecords, int start = 0, int length = 1, string order = "ID ASC", string orderId = null)
        {
            List<OrderDBO> result = new List<OrderDBO>();
            filteredRecords = 0;
            totalRecords = 0;
            try
            {
                using (SqlConnection cnn = new SqlConnection(DBAccess.DBConnectionString))
                {
                    using (SqlCommand cmd = cnn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT ID, ARRIVAL_DATE, ESTIMATED_COMPLETION_DATE, WORKLOADS, MACHINE_IDS FROM ORDERS ";
                        if (!string.IsNullOrWhiteSpace(orderId))
                        {
                            cmd.CommandText += "WHERE ID = @id";
                            cmd.Parameters.Add(DBAccess.CustomSQLParameter("id", orderId));
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(order)) order = "ID ASC";
                            cmd.CommandText += DBAccess.DatatableFilter(start, length, order);
                        }
                        cmd.CommandType = CommandType.Text;
                        cnn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                OrderDBO item = new OrderDBO()
                                {
                                    Id = reader.GetString(0),
                                    ArrivalDate = reader.GetDateTime(1),
                                    EstimatedCompletionDate = reader.GetDateTime(2),
                                    Workloads = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Workload>>(reader.GetString(3)),
                                    MachineIds = Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>>(reader.GetString(4)),
                                };
                                result.Add(item);
                            }
                        }
                    }
                }
                if (string.IsNullOrWhiteSpace(orderId))
                {
                    totalRecords = DBAccess.CountRegistersByCondition("ORDERS", "");
                    filteredRecords = totalRecords; //There's no filter for this method
                }
            }
            catch (Exception ex) { }
            return result;
        }

        internal string Insert(OrderDBO item)
        {
            if (!item.ValidateInsert())
                return null;
            string result = null;
            try
            {
                if (item.ArrivalDate == new DateTime() || item.ArrivalDate == DateTime.MaxValue)
                    item.ArrivalDate = DateTime.UtcNow;

                using (SqlConnection cnn = new SqlConnection(DBAccess.DBConnectionString))
                {
                    using (SqlCommand cmd = cnn.CreateCommand())
                    {
                        cmd.CommandText = $@"IF NOT EXISTS (SELECT TOP 1 ID FROM ORDERS WHERE ID = @checkId)
BEGIN
    {OUTPUT_START_STRING}
    INSERT INTO ORDERS (ID, ARRIVAL_DATE, ESTIMATED_COMPLETION_DATE, WORKLOADS, MACHINE_IDS)
    output inserted.ID into @outputResult 
    VALUES (@ID, @ARRIVAL_DATE, @ESTIMATED_COMPLETION_DATE, @WORKLOADS, @MACHINE_IDS)
    {OUTPUT_END}
END
ELSE BEGIN SELECT NULL END";
                        cmd.Parameters.Add(DBAccess.CustomSQLParameter("@checkId", item.Id));
                        cmd.Parameters.Add(DBAccess.CustomSQLParameter("@ID", item.Id));
                        cmd.Parameters.Add(DBAccess.CustomSQLParameter("@ARRIVAL_DATE", item.ArrivalDate));
                        cmd.Parameters.Add(DBAccess.CustomSQLParameter("@ESTIMATED_COMPLETION_DATE", item.EstimatedCompletionDate));
                        cmd.Parameters.Add(DBAccess.CustomSQLParameter("@WORKLOADS", Newtonsoft.Json.JsonConvert.SerializeObject(item.Workloads)));
                        cmd.Parameters.Add(DBAccess.CustomSQLParameter("@MACHINE_IDS", Newtonsoft.Json.JsonConvert.SerializeObject(item.MachineIds)));

                        cmd.CommandType = CommandType.Text;
                        cnn.Open();
                        result = (string)cmd.ExecuteScalar();
                        cnn.Close();
                    }
                }
            }
            catch (Exception ex) { }
            return result;
        }

        internal bool ReplaceMachine(string orderId, int machineIdToRemove, int machineIdToAdd)
        {
            if (string.IsNullOrWhiteSpace(orderId) || machineIdToAdd <= 0 || machineIdToRemove <= 0) return false;
            var order = Get(out _, out _, orderId: orderId).FirstOrDefault();
            if (order == null) return false;

            if ((order.MachineIds as List<int>).Remove(machineIdToRemove))
            {
                (order.MachineIds as List<int>).Add(machineIdToAdd);
                SetMachineIdsColumn(orderId, order.MachineIds.Distinct());
            }
            return true;
        }

        private bool SetMachineIdsColumn(string orderId, IEnumerable<int> machineIds)
        {
            int rowsAffected = 0;
            try
            {
                using (SqlConnection cnn = new SqlConnection(DBAccess.DBConnectionString))
                {
                    using (SqlCommand cmd = cnn.CreateCommand())
                    {
                        cmd.CommandText = $@"UPDATE ORDERS SET MACHINE_IDS = @MACHINE_IDS WHERE ID = @ID";
                        cmd.Parameters.Add(DBAccess.CustomSQLParameter("@ID", orderId));
                        cmd.Parameters.Add(DBAccess.CustomSQLParameter("@MACHINE_IDS", Newtonsoft.Json.JsonConvert.SerializeObject(machineIds)));

                        cmd.CommandType = CommandType.Text;
                        cnn.Open();
                        rowsAffected = cmd.ExecuteNonQuery();
                        cnn.Close();
                    }
                }
            }
            catch (Exception) { }
            return rowsAffected > 0;
        }

        internal bool RemoveMachine(string orderId, int machineId)
        {
            if (string.IsNullOrWhiteSpace(orderId) || machineId <= 0) return false;
            var order = Get(out _, out _, orderId: orderId).FirstOrDefault();
            if (order == null) return false;
            if ((order.MachineIds as List<int>).Remove(machineId))
                SetMachineIdsColumn(orderId, order.MachineIds.Distinct());
            return true;
        }

        internal bool AddMachine(string orderId, int machineId)
        {
            if (string.IsNullOrWhiteSpace(orderId) || machineId <= 0) return false;
            var order = Get(out _, out _, orderId: orderId).FirstOrDefault();
            if (order == null) return false;
            (order.MachineIds as List<int>).Add(machineId);
            SetMachineIdsColumn(orderId, order.MachineIds.Distinct());
            return true;
        }

        internal void UpdateEstimatedCompletionDate(string orderId, DateTime newDatetime, bool onlyIfGreater = false)
        {
            if (onlyIfGreater)
            {
                var order = Get(out _, out _, orderId: orderId).FirstOrDefault();
                if (order == null || order.EstimatedCompletionDate < newDatetime) return;
            }

            using (SqlConnection cnn = new SqlConnection(DBAccess.DBConnectionString))
            {
                using (SqlCommand cmd = cnn.CreateCommand())
                {
                    cmd.CommandText = $@"UPDATE ORDERS SET ESTIMATED_COMPLETION_DATE = @ESTIMATED_COMPLETION_DATE WHERE ID = @ID";
                    cmd.Parameters.Add(DBAccess.CustomSQLParameter("@ID", orderId));
                    cmd.Parameters.Add(DBAccess.CustomSQLParameter("@ESTIMATED_COMPLETION_DATE", newDatetime));

                    cmd.CommandType = CommandType.Text;
                    cnn.Open();
                    _ = cmd.ExecuteNonQuery();
                    cnn.Close();
                }
            }
        }
    }
}
