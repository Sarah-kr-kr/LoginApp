using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace LoginApp.DAL
{
    public static class DbHelper
    {
        private static readonly string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=LoginDB;Integrated Security=True";

        public static SqlConnection Connection = new SqlConnection(connectionString);

        public static void OpenConnection()
        {
            if (Connection.State != System.Data.ConnectionState.Open)
                Connection.Open();
        }

        public static void CloseConnection()
        {
            if (Connection.State != System.Data.ConnectionState.Closed)
                Connection.Close();
        }
    }
}
