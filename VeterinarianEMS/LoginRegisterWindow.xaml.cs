using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
            LoadPositions();
        }

        // ================= LOAD POSITIONS =================
        private void LoadPositions()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT PositionID, PositionName FROM empositions";
                    using (SqlDataAdapter da = new SqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        cmbPosition.ItemsSource = dt.DefaultView;
                        cmbPosition.DisplayMemberPath = "PositionName";
                        cmbPosition.SelectedValuePath = "PositionID";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading positions: " + ex.Message);
            }
        }

        // ================= TRANSITIONS =================
        private void btnShowRegister_Click(object sender, RoutedEventArgs e)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
            fadeOut.Completed += (s, args) =>
            {
                DescriptionView.Visibility = Visibility.Collapsed;
                RegisterView.Visibility = Visibility.Visible;

                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4));
                RegisterView.BeginAnimation(OpacityProperty, fadeIn);
            };
            DescriptionView.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void btnBackToInfo_Click(object sender, RoutedEventArgs e)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
            fadeOut.Completed += (s, args) =>
            {
                RegisterView.Visibility = Visibility.Collapsed;
                DescriptionView.Visibility = Visibility.Visible;

                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4));
                DescriptionView.BeginAnimation(OpacityProperty, fadeIn);

                // Reset form fields
                txtRegUsername.Text = "";
                txtRegPassword.Password = "";
                txtRegConfirmPassword.Password = "";
                txtRegPasswordVisible.Text = "";
                txtRegConfirmPasswordVisible.Text = "";
                RectStrength1.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DDD"));
                RectStrength2.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DDD"));
                RectStrength3.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DDD"));
                RectMatch.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DDD"));
                lblStrength.Text = "";
                lblMatch.Text = "No Match";
                lblMatch.Foreground = new SolidColorBrush(Colors.Gray);
            };
            RegisterView.BeginAnimation(OpacityProperty, fadeOut);
        }

        // ================= REGISTER PASSWORD SHOW =================
        private void btnShowRegPass_Checked(object sender, RoutedEventArgs e)
        {
            txtRegPasswordVisible.Text = txtRegPassword.Password;
            txtRegPassword.Visibility = Visibility.Collapsed;
            txtRegPasswordVisible.Visibility = Visibility.Visible;
        }

        private void btnShowRegPass_Unchecked(object sender, RoutedEventArgs e)
        {
            txtRegPassword.Password = txtRegPasswordVisible.Text;
            txtRegPasswordVisible.Visibility = Visibility.Collapsed;
            txtRegPassword.Visibility = Visibility.Visible;
        }

        // ================= CONFIRM PASSWORD SHOW =================
        private void btnShowConfirmPass_Checked(object sender, RoutedEventArgs e)
        {
            txtRegConfirmPasswordVisible.Text = txtRegConfirmPassword.Password;
            txtRegConfirmPassword.Visibility = Visibility.Collapsed;
            txtRegConfirmPasswordVisible.Visibility = Visibility.Visible;
        }

        private void btnShowConfirmPass_Unchecked(object sender, RoutedEventArgs e)
        {
            txtRegConfirmPassword.Password = txtRegConfirmPasswordVisible.Text;
            txtRegConfirmPasswordVisible.Visibility = Visibility.Collapsed;
            txtRegConfirmPassword.Visibility = Visibility.Visible;
        }

        // ================= LOGIN PASSWORD SHOW =================
        private void btnShowLoginPass_Checked(object sender, RoutedEventArgs e)
        {
            txtLoginPasswordVisible.Text = txtLoginPassword.Password;
            txtLoginPassword.Visibility = Visibility.Collapsed;
            txtLoginPasswordVisible.Visibility = Visibility.Visible;
        }

        private void btnShowLoginPass_Unchecked(object sender, RoutedEventArgs e)
        {
            txtLoginPassword.Password = txtLoginPasswordVisible.Text;
            txtLoginPasswordVisible.Visibility = Visibility.Collapsed;
            txtLoginPassword.Visibility = Visibility.Visible;
        }

        // ================= SYNC VISIBLE PASSWORD TEXT =================
        private void txtRegPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtRegPassword.Password = txtRegPasswordVisible.Text;
        }

        private void txtRegConfirmPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtRegConfirmPassword.Password = txtRegConfirmPasswordVisible.Text;
        }

        private void txtLoginPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtLoginPassword.Password = txtLoginPasswordVisible.Text;
        }

        // ================= PASSWORD STRENGTH =================
        private void txtRegPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            string password = txtRegPassword.Password;
            int score = 0;

            if (password.Length >= 6) score++;
            if (Regex.IsMatch(password, "[A-Z]")) score++;
            if (Regex.IsMatch(password, "[0-9]")) score++;

            RectStrength1.Fill = Brushes.LightGray;
            RectStrength2.Fill = Brushes.LightGray;
            RectStrength3.Fill = Brushes.LightGray;

            if (score == 0 || password.Length == 0)
            {
                lblStrength.Text = "";
            }
            else if (score == 1)
            {
                RectStrength1.Fill = Brushes.Red;
                lblStrength.Text = "Weak";
                lblStrength.Foreground = Brushes.Red;
            }
            else if (score == 2)
            {
                RectStrength1.Fill = Brushes.Orange;
                RectStrength2.Fill = Brushes.Orange;
                lblStrength.Text = "Medium";
                lblStrength.Foreground = Brushes.Orange;
            }
            else if (score == 3)
            {
                RectStrength1.Fill = Brushes.Green;
                RectStrength2.Fill = Brushes.Green;
                RectStrength3.Fill = Brushes.Green;
                lblStrength.Text = "Strong";
                lblStrength.Foreground = Brushes.Green;
            }
        }

        // ================= PASSWORD MATCH =================
        private void txtRegConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtRegConfirmPassword.Password) &&
                txtRegConfirmPassword.Password == txtRegPassword.Password)
            {
                RectMatch.Fill = Brushes.Green;
                lblMatch.Text = "Match ✓";
                lblMatch.Foreground = Brushes.Green;
            }
            else
            {
                RectMatch.Fill = Brushes.Red;
                lblMatch.Text = "No Match";
                lblMatch.Foreground = Brushes.Red;
            }
        }

        // ================= REGISTER =================
        private void Register_Click(object sender, RoutedEventArgs e)
        {
            string username = txtRegUsername.Text.Trim();
            string password = txtRegPassword.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Username and Password are required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbPosition.SelectedValue == null)
            {
                MessageBox.Show("Please select a position.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (password != txtRegConfirmPassword.Password)
            {
                MessageBox.Show("Passwords do not match.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string check = "SELECT COUNT(*) FROM Users WHERE Username=@u";
                    using (SqlCommand checkCmd = new SqlCommand(check, conn))
                    {
                        checkCmd.Parameters.Add("@u", SqlDbType.VarChar).Value = username;
                        if ((int)checkCmd.ExecuteScalar() > 0)
                        {
                            MessageBox.Show("Username already exists. Please choose another.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    string hashed = BCrypt.Net.BCrypt.HashPassword(password);
                    string insert = @"INSERT INTO Users (Username, Password, PositionID, EmployeeID)
                                      VALUES (@u, @p, @pos, NULL)";

                    using (SqlCommand cmd = new SqlCommand(insert, conn))
                    {
                        cmd.Parameters.Add("@u", SqlDbType.VarChar).Value = username;
                        cmd.Parameters.Add("@p", SqlDbType.VarChar).Value = hashed;
                        cmd.Parameters.Add("@pos", SqlDbType.Int).Value = cmbPosition.SelectedValue;
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Account created successfully! You can now log in.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                btnBackToInfo_Click(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Registration Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ================= LOGIN =================
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = txtLoginUsername.Text.Trim();
            string password = txtLoginPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter your username and password.",
                                "Login Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
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

                                if (BCrypt.Net.BCrypt.Verify(password, storedHash))
                                {
                                    // Store session info
                                    UserSession.Username = username;
                                    UserSession.Role = positionName;
                                    UserSession.UserId = Convert.ToInt32(reader["UserID"]);
                                    UserSession.EmployeeID = reader["EmployeeID"] != DBNull.Value
                                        ? Convert.ToInt32(reader["EmployeeID"])
                                        : (int?)null;

                                    // Close reader before opening new window
                                    reader.Close();

                                    // Redirect based on role
                                    if (positionName.IndexOf("HR", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        new MainWindow().Show();
                                    }
                                    else
                                    {
                                        new EmpMainWindow().Show();
                                    }

                                    this.Close();
                                    return;
                                }
                            }
                        }
                    }
                }

                // Reached here means credentials were wrong
                MessageBox.Show("Invalid username or password.",
                                "Login Failed",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message,
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        // ================= CLOSE =================
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}