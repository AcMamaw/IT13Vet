using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using BCrypt.Net;
using static VeterinarianEMS.MainWindow;

namespace VeterinarianEMS.Views
{
    public partial class LoginRegisterWindow : Window
    {
        private readonly string connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True;";

        public LoginRegisterWindow()
        {
            InitializeComponent();
        }



        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = txtLoginUsername.Text.Trim();
            string password = txtLoginPassword.Visibility == Visibility.Visible
                              ? txtLoginPassword.Password
                              : txtLoginPasswordVisible.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter your username and password.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                SELECT u.UserID, u.EmployeeID, u.Password, p.PositionName
                FROM Users u
                INNER JOIN empositions p ON u.PositionID = p.PositionID
                WHERE u.Username = @username";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@username", SqlDbType.VarChar, 100).Value = username;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string storedHash = reader["Password"].ToString();
                                string positionName = reader["PositionName"].ToString();

                                // ✅ Check password with BCrypt
                                if (BCrypt.Net.BCrypt.Verify(password, storedHash))
                                {
                                    // ✅ Store user info in session
                                    UserSession.Username = username;
                                    UserSession.Role = positionName;
                                    UserSession.UserId = Convert.ToInt32(reader["UserID"]);

                                    if (reader["EmployeeID"] != DBNull.Value)
                                        UserSession.EmployeeID = Convert.ToInt32(reader["EmployeeID"]);
                                    else
                                        UserSession.EmployeeID = null;

                                    // ✅ Continue to next window
                                    if (positionName.IndexOf("HR", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        MainWindow main = new MainWindow();
                                        main.Show();
                                    }
                                    else
                                    {
                                        EmpMainWindow emp = new EmpMainWindow();
                                        emp.Show();
                                    }

                                    this.Close();
                                    return;
                                }
                                else
                                {
                                    MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Database error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        #region Close Button
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        #endregion

        #region Password Visibility Toggle
        private void TogglePasswordVisibility(PasswordBox pwdBox, TextBox txtBox, bool show)
        {
            if (show)
            {
                txtBox.Text = pwdBox.Password;
                pwdBox.Visibility = Visibility.Collapsed;
                txtBox.Visibility = Visibility.Visible;
            }
            else
            {
                pwdBox.Password = txtBox.Text;
                txtBox.Visibility = Visibility.Collapsed;
                pwdBox.Visibility = Visibility.Visible;
            }
        }

        private void LoginPassword_Show_Checked(object sender, RoutedEventArgs e) =>
            TogglePasswordVisibility(txtLoginPassword, txtLoginPasswordVisible, true);

        private void LoginPassword_Show_Unchecked(object sender, RoutedEventArgs e) =>
            TogglePasswordVisibility(txtLoginPassword, txtLoginPasswordVisible, false);
        #endregion
    }
}
