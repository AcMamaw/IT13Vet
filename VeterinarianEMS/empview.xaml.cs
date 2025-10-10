using System;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace VeterinarianEMS.Controls
{
    public partial class EmpView : UserControl
    {
        // Corrected connection string
        private string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

        public EmpView()
        {
            InitializeComponent();
        }

        public void LoadEmployee(int employeeId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Main employee query with shift info
                    string query = @"
                SELECT e.FirstName, e.MiddleName, e.LastName, e.Sex, e.Age, e.ContactNumber,
                       e.Address, e.BaseSalary, e.HireDate, e.DOB,
                       p.PositionName, d.DepartmentName,
                       s.ShiftName, s.StartTime, s.EndTime, es.ShiftDays
                FROM employees e
                LEFT JOIN empositions p ON e.PositionID = p.PositionID
                LEFT JOIN department d ON e.DepartmentID = d.DepartmentID
                LEFT JOIN employeeshifts es ON e.EmployeeID = es.EmployeeID
                LEFT JOIN shifts s ON es.ShiftID = s.ShiftID
                WHERE e.EmployeeID = @EmployeeID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeID", employeeId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Full Name
                                string first = reader["FirstName"]?.ToString() ?? "";
                                string middle = reader["MiddleName"]?.ToString() ?? "";
                                string last = reader["LastName"]?.ToString() ?? "";
                                FullNameTextBlock.Text = $"{first} {middle} {last}".Replace("  ", " ").Trim();

                                // Basic Info
                                SexTextBlock.Text = reader["Sex"]?.ToString() ?? "-";
                                AgeTextBlock.Text = reader["Age"] != DBNull.Value ? reader["Age"].ToString() : "-";

                                // Contact & Address
                                ContactTextBlock.Text = reader["ContactNumber"]?.ToString() ?? "-";
                                AddressTextBlock.Text = reader["Address"]?.ToString() ?? "-";
                                DOBTextBlock.Text = reader["DOB"] != DBNull.Value ? ((DateTime)reader["DOB"]).ToShortDateString() : "-";

                                // Department / Position
                                PositionTextBlock.Text = reader["PositionName"]?.ToString() ?? "-";
                                DepartmentTextBlock.Text = reader["DepartmentName"]?.ToString() ?? "-";

                                // Salary
                                SalaryTextBlock.Text = reader["BaseSalary"] != DBNull.Value ? Convert.ToDecimal(reader["BaseSalary"]).ToString("C") : "-";
                                HireDateTextBlock.Text = reader["HireDate"] != DBNull.Value ? ((DateTime)reader["HireDate"]).ToShortDateString() : "-";

                                // Shift Info
                                ShiftNameTextBlock.Text = reader["ShiftName"]?.ToString() ?? "-";
                                ShiftStartTextBlock.Text = reader["StartTime"] != DBNull.Value ? ((TimeSpan)reader["StartTime"]).ToString(@"hh\:mm") : "-";
                                ShiftEndTextBlock.Text = reader["EndTime"] != DBNull.Value ? ((TimeSpan)reader["EndTime"]).ToString(@"hh\:mm") : "-";
                                ShiftDaysTextBlock.Text = reader["ShiftDays"]?.ToString() ?? "-";
                            }
                            else
                            {
                                MessageBox.Show("Employee not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading employee:\n{ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (this.Parent is Window parentWindow)
            {
                parentWindow.Close();
            }
        }
    }
}
