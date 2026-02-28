using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VeterinarianEMS.Controls;
using VeterinarianEMS.Views;
using static VeterinarianEMS.MainWindow;

namespace VeterinarianEMS
{
    public partial class EmployeeProfileControl : UserControl
    {
        public event Action EmployeeProfileSaved;
        private readonly string _connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True;";

        private List<EmployeePayslipViewModel> _payslips = new List<EmployeePayslipViewModel>();

        public EmployeeProfileControl()
        {
            InitializeComponent();
            LoadEmployeeProfile();
            LoadEmployeePayslips();
        }

        #region Employee Profile

        private void LoadEmployeeProfile()
        {
            if (UserSession.EmployeeID == null)
            {
                MessageBox.Show("No employee logged in.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT e.EmployeeID, e.FirstName, e.MiddleName, e.LastName,
                               e.Email, e.ContactNumber, e.HireDate,
                               p.PositionName, d.DepartmentName, e.PhotoPath
                        FROM employees e
                        LEFT JOIN empositions p ON e.PositionID = p.PositionID
                        LEFT JOIN department d ON e.DepartmentID = d.DepartmentID
                        WHERE e.EmployeeID = @EmployeeID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeID", UserSession.EmployeeID);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string fullName = $"{reader["FirstName"]} {reader["MiddleName"]} {reader["LastName"]}".Replace("  ", " ");
                                FullNameText.Text = fullName;
                                EmployeeIDText.Text = $"Employee ID: {reader["EmployeeID"]}";
                                PositionText.Text = $"Position: {reader["PositionName"]}";
                                DepartmentText.Text = $"Department: {reader["DepartmentName"]}";
                                EmailText.Text = $"Email: {reader["Email"]}";
                                PhoneText.Text = $"Phone: {reader["ContactNumber"]}";
                                HireDateText.Text = $"Hire Date: {Convert.ToDateTime(reader["HireDate"]).ToString("MMMM dd, yyyy")}";

                                if (reader["PhotoPath"] != DBNull.Value)
                                {
                                    string path = reader["PhotoPath"].ToString();
                                    if (File.Exists(path))
                                    {
                                        ProfileImage.Source = new BitmapImage(new Uri(path));
                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show("Employee record not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading employee data: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Payslip
        private void LoadEmployeePayslips()
        {
            if (UserSession.EmployeeID == null) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT PayPeriodStart, PayPeriodEnd, TotalHoursWorked, LeaveDays, GrossPay
                        FROM vw_EmployeePayslip
                        WHERE EmployeeID = @EmployeeID
                        ORDER BY PayPeriodStart DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeID", UserSession.EmployeeID);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            _payslips.Clear();
                            while (reader.Read())
                            {
                                _payslips.Add(new EmployeePayslipViewModel
                                {
                                    PayPeriodStart = reader.GetDateTime(0),
                                    PayPeriodEnd = reader.GetDateTime(1),
                                    TotalHoursWorked = Convert.ToDecimal(reader["TotalHoursWorked"]),
                                    LeaveDays = Convert.ToDecimal(reader["LeaveDays"]),
                                    GrossPay = Convert.ToDecimal(reader["GrossPay"])
                                });
                            }
                        }
                    }
                }

                PayslipDataGrid.ItemsSource = _payslips;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading payslips: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PayslipSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = PayslipSearchTextBox.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(keyword))
            {
                PayslipDataGrid.ItemsSource = _payslips;
            }
            else
            {
                var filtered = _payslips
                    .Where(p => p.PayPeriodStart.ToString("yyyy-MM-dd").Contains(keyword) ||
                                p.PayPeriodEnd.ToString("yyyy-MM-dd").Contains(keyword))
                    .ToList();

                PayslipDataGrid.ItemsSource = filtered;
            }
        }

        #endregion

        #region Profile Image Upload

        private void ProfileImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (dlg.ShowDialog() == true)
            {
                string selectedFile = dlg.FileName;
                string destFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProfileImages");
                Directory.CreateDirectory(destFolder);
                string destPath = Path.Combine(destFolder, Path.GetFileName(selectedFile));

                try
                {
                    File.Copy(selectedFile, destPath, true);
                    ProfileImage.Source = new BitmapImage(new Uri(destPath));

                    using (SqlConnection conn = new SqlConnection(_connectionString))
                    {
                        conn.Open();
                        string updateQuery = "UPDATE employees SET PhotoPath = @Photo WHERE EmployeeID = @EmployeeID";
                        using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@Photo", destPath);
                            cmd.Parameters.AddWithValue("@EmployeeID", UserSession.EmployeeID);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Profile photo updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error updating photo: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region View/Edit Buttons

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            int employeeId = UserSession.EmployeeID ?? 0;
            if (employeeId == 0)
            {
                MessageBox.Show("No employee logged in.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editProfileControl = new EmpEditProfileControl(employeeId);

            // Subscribe to the event
            editProfileControl.EmployeeProfileSaved += () =>
            {
                LoadEmployeeProfile(); // refresh main view after saving
            };

            var window = new Window
            {
                Content = editProfileControl,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Width = 750,
                Height = 700,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ShowInTaskbar = false,
                Topmost = true
            };

            window.Opacity = 0;
            window.Loaded += (s, e2) =>
            {
                var fade = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
                window.BeginAnimation(Window.OpacityProperty, fade);
            };

            window.ShowDialog();
        }
        private void ShiftButton_Click(object sender, RoutedEventArgs e)
        {
            int employeeId = UserSession.EmployeeID ?? 0;
            if (employeeId == 0)
            {
                MessageBox.Show("No employee logged in.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create the EmpScheduleControl to show the employee's schedule
            var shiftControl = new EmpScheduleControl();
            shiftControl.LoadEmployeeShift(employeeId);

            // Create hosting window
            var window = new Window
            {
                Content = shiftControl,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Width = 700,
                Height = 720,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ShowInTaskbar = false,
                Topmost = true
            };

            // Fade-in animation
            window.Opacity = 0;
            window.Loaded += (s, e2) =>
            {
                var fade = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
                window.BeginAnimation(Window.OpacityProperty, fade);
            };

            window.ShowDialog();
        }

        #endregion
    }

    public class EmployeePayslipViewModel
    {
        public DateTime PayPeriodStart { get; set; }
        public DateTime PayPeriodEnd { get; set; }
        public decimal TotalHoursWorked { get; set; }
        public decimal LeaveDays { get; set; }
        public decimal GrossPay { get; set; }
    }
}
