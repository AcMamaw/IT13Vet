using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using static VeterinarianEMS.MainWindow;

namespace VeterinarianEMS
{
    public partial class EmployeeProfileControl : UserControl
    {
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
