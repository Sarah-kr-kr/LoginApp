using LoginApp.DAL;
using LoginApp.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LoginApp
{
    public partial class LoginForm : Form
    {
        // الثوابت المحددة حسب المحاضرة
        private const int MaxAttemptsLevel1 = 3; // تنبيه
        private const int MaxAttemptsLevel2 = 4; // قفل ليوم
        private const int MaxAttemptsLevel3 = 5; // قفل ليومين

        public LoginForm()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;
            string hashedPassword = SecurityHelper.ComputeSHA256(password);

            // التحقق من حالة قفل الحساب
            if (IsAccountLocked(username))
            {
                MessageBox.Show("Your account is temporarily locked due to too many failed login attempts.");
                return;
            }

            try
            {
                DbHelper.OpenConnection();

                string query = "SELECT COUNT(*) FROM Users WHERE Username = @u AND PasswordHash = @p";
                SqlCommand cmd = new SqlCommand(query, DbHelper.Connection);
                cmd.Parameters.AddWithValue("@u", username);
                cmd.Parameters.AddWithValue("@p", hashedPassword);

                int count = (int)cmd.ExecuteScalar();

                if (count == 1)
                {
                    MessageBox.Show("Login successful.");
                    ResetFailedAttempts(username); // إعادة التصفير عند الدخول الصحيح
                    Application.Exit();
                }
                else
                {
                    UpdateFailedAttempts(username);
                    int currentAttempts = GetFailedAttempts(username);
                    lblAttempts.Text = $"Attempts left: {MaxAttemptsLevel3 - currentAttempts}";

                    if (currentAttempts >= MaxAttemptsLevel3)
                    {
                        MessageBox.Show("Too many failed attempts. Application will now close.");
                        Application.Exit();
                    }
                    else if (currentAttempts >= MaxAttemptsLevel1)
                    {
                        MessageBox.Show("Warning: multiple failed login attempts!");
                    }
                    else
                    {
                        MessageBox.Show("Invalid username or password.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                DbHelper.CloseConnection();
            }
        }

        private bool IsAccountLocked(string username)
        {
            using (SqlConnection conn = new SqlConnection(DbHelper.Connection.ConnectionString))
            {
                string query = @"SELECT FailedAttempts, LastAttempt FROM Users WHERE Username = @Username";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Username", username);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    int attempts = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                    DateTime lastAttempt = reader.IsDBNull(1) ? DateTime.MinValue : reader.GetDateTime(1);
                    TimeSpan timeSinceLast = DateTime.Now - lastAttempt;

                    if ((attempts >= MaxAttemptsLevel2 && timeSinceLast.TotalHours < 24) ||
                        (attempts >= MaxAttemptsLevel3 && timeSinceLast.TotalHours < 48))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void UpdateFailedAttempts(string username)
        {
            using (SqlConnection conn = new SqlConnection(DbHelper.Connection.ConnectionString))
            {
                string query = @"UPDATE Users
                                 SET FailedAttempts = ISNULL(FailedAttempts, 0) + 1,
                                     LastAttempt = GETDATE()
                                 WHERE Username = @Username";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Username", username);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private int GetFailedAttempts(string username)
        {
            using (SqlConnection conn = new SqlConnection(DbHelper.Connection.ConnectionString))
            {
                string query = "SELECT FailedAttempts FROM Users WHERE Username = @Username";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Username", username);

                conn.Open();
                object result = cmd.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                    return Convert.ToInt32(result);

                return 0;
            }
        }

        private void ResetFailedAttempts(string username)
        {
            using (SqlConnection conn = new SqlConnection(DbHelper.Connection.ConnectionString))
            {
                string query = @"UPDATE Users
                                 SET FailedAttempts = 0,
                                     LastAttempt = NULL
                                 WHERE Username = @Username";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Username", username);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}