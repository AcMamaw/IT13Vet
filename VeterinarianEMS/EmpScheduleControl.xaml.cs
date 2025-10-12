using System;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace VeterinarianEMS
{
    public partial class EmpScheduleControl : UserControl
    {
        private readonly string connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True";

        public EmpScheduleControl()
        {
            InitializeComponent();
        }

        public void LoadEmployeeShift(int employeeId)
        {
            try
            {
                string query = @"
                    SELECT s.ShiftName, s.StartTime, s.EndTime, es.ShiftDays
                    FROM employeeshifts es
                    INNER JOIN shifts s ON es.ShiftID = s.ShiftID
                    WHERE es.EmployeeID = @EmployeeID";

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeID", employeeId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Read Shift Info
                                txtShiftName.Text = reader["ShiftName"]?.ToString() ?? "-";

                                // Handle TimeSpan correctly
                                if (reader["StartTime"] != DBNull.Value)
                                {
                                    var startTime = (TimeSpan)reader["StartTime"];
                                    txtStartTime.Text = DateTime.Today.Add(startTime).ToString("hh:mm tt");
                                }

                                if (reader["EndTime"] != DBNull.Value)
                                {
                                    var endTime = (TimeSpan)reader["EndTime"];
                                    txtEndTime.Text = DateTime.Today.Add(endTime).ToString("hh:mm tt");
                                }

                                txtShiftDays.Text = reader["ShiftDays"]?.ToString() ?? "-";
                            }
                            else
                            {
                                MessageBox.Show("No shift assigned to this employee.", "Information",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading shift details:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);
            parentWindow?.Close();
        }
    }
}
