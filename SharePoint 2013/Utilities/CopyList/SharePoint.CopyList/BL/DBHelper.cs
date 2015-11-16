using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePoint.CopyList.BL {
    public class DBHelper {

        public DBHelper(string connectionString) {
            connect = new SqlConnection(connectionString);
        }

        private SqlConnection connect = null;

        public void OpenConnection()
        {
            connect.Open();
        }

        public void CloseConnection()
        {
            connect.Close();
        }

        public int SetListNextItemId(int id, Guid listId) {
            int status = -1;
            string sql = string.Format("UPDATE AllListsAux set NextAvailableId=@ItemId where ListID = @ListId");
            using (SqlCommand cmd = new SqlCommand(sql, this.connect)) {
                try {
                    OpenConnection();
                    cmd.Parameters.AddWithValue("@ItemId", id);
                    cmd.Parameters.AddWithValue("@ListId", listId);
                    status = cmd.ExecuteNonQuery();
                    return status;
                }
                catch (SqlException ex) {
                    Console.WriteLine(ex.Message);
                    return status;
                }
                finally {
                    CloseConnection();
                }
            }
        }

        public int GetListNextItemId(Guid listId) {
            int id = -1;
            string sql = string.Format("select NextAvailableId from AllListsAux where ListID = @ListId");
            using (SqlCommand cmd = new SqlCommand(sql, this.connect)) {
                try {
                    OpenConnection();
                    cmd.Parameters.AddWithValue("@ListId", listId);
                    cmd.ExecuteNonQuery();
                    id = (int)cmd.ExecuteScalar();
                    return id;
                }
                catch (SqlException ex) {
                    Console.WriteLine(ex.Message);
                    return id;
                }
                finally {
                    CloseConnection();
                }
            }
        }

    }
}
