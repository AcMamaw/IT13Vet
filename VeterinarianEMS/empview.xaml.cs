using Microsoft.Data.SqlClient;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace VeterinarianEMS.Controls
{
    public partial class EmpView : UserControl
    {
        private int _currentEmployeeId;
        private int _currentRating = 0;
        private bool _hasRated = false;
        private string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

        public EmpView()
        {
            InitializeComponent();
        }

        public void LoadEmployee(int employeeId)
        {
            _currentEmployeeId = employeeId;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

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
                                string first = reader["FirstName"]?.ToString() ?? "";
                                string middle = reader["MiddleName"]?.ToString() ?? "";
                                string last = reader["LastName"]?.ToString() ?? "";
                                FullNameTextBlock.Text = $"{first} {middle} {last}".Replace("  ", " ").Trim();

                                SexTextBlock.Text = reader["Sex"]?.ToString() ?? "-";
                                AgeTextBlock.Text = reader["Age"] != DBNull.Value ? reader["Age"].ToString() : "-";
                                ContactTextBlock.Text = reader["ContactNumber"]?.ToString() ?? "-";
                                AddressTextBlock.Text = reader["Address"]?.ToString() ?? "-";
                                DOBTextBlock.Text = reader["DOB"] != DBNull.Value ? ((DateTime)reader["DOB"]).ToShortDateString() : "-";

                                PositionTextBlock.Text = reader["PositionName"]?.ToString() ?? "-";
                                DepartmentTextBlock.Text = reader["DepartmentName"]?.ToString() ?? "-";

                                SalaryTextBlock.Text = reader["BaseSalary"] != DBNull.Value ? Convert.ToDecimal(reader["BaseSalary"]).ToString("C") : "-";
                                HireDateTextBlock.Text = reader["HireDate"] != DBNull.Value ? ((DateTime)reader["HireDate"]).ToShortDateString() : "-";

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

        // ⭐ Star click
        private void Star_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock clickedStar)
            {
                int rating = int.Parse(clickedStar.Tag.ToString());
                _currentRating = rating;
                _hasRated = true;

                foreach (TextBlock star in StarPanel.Children)
                {
                    int starNumber = int.Parse(star.Tag.ToString());
                    star.Foreground = starNumber <= _currentRating ? Brushes.Gold : Brushes.Gray;
                }

                // Switch buttons
                CloseButton.Visibility = Visibility.Collapsed;
                SubmitButton.Visibility = Visibility.Visible;
                CancelButton.Visibility = Visibility.Visible;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset stars
            foreach (TextBlock star in StarPanel.Children)
            {
                star.Foreground = Brushes.Gray;
            }

            _currentRating = 0;
            _hasRated = false;

            // Restore default button
            SubmitButton.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Collapsed;
            CloseButton.Visibility = Visibility.Visible;
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            SaveEmployeeRating(_currentRating);

            if (this.Parent is Window parentWindow)
                parentWindow.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.Parent is Window parentWindow)
                parentWindow.Close();
        }


        private void SaveEmployeeRating(int rating)
        {
            // ✅ Check if an employee is loaded
            if (_currentEmployeeId <= 0)
            {
                MessageBox.Show("No employee selected. Please load an employee first.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                UPDATE employees
                SET EmployeeRating = @Rating
                WHERE EmployeeID = @EmployeeID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Rating", rating);
                        cmd.Parameters.AddWithValue("@EmployeeID", _currentEmployeeId);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show($"Employee rated {rating} star(s) successfully!",
                                            "Rating Saved", MessageBoxButton.OK, MessageBoxImage.Information);

                            // Reset rating after saving
                            _currentRating = 0;
                            _hasRated = false;
                            foreach (TextBlock star in StarPanel.Children)
                            {
                                star.Foreground = Brushes.Gray;
                            }
                            SubmitButton.Visibility = Visibility.Collapsed;
                            CancelButton.Visibility = Visibility.Collapsed;
                            CloseButton.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            MessageBox.Show("Failed to save rating. Employee not found in database.",
                                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving rating:\n{ex.Message}",
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
