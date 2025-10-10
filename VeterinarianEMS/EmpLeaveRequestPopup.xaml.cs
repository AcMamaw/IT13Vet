using Microsoft.Data.SqlClient;
using System;
using System.Windows;
using System.Windows.Controls;
using static VeterinarianEMS.MainWindow; // for UserSession

namespace VeterinarianEMS.Controls
{
    public partial class LeaveRequestPopup : UserControl
    {
        public event Action<bool> OnClose;

        private string connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True;";

        public int? EditingId { get; set; } = null; // null = insert, value = update

        public LeaveRequestPopup()
        {
            InitializeComponent();
        }

        // 🔹 Get the EmployeeID for the logged-in user
        private int GetLoggedInEmployeeID()
        {
            if (UserSession.EmployeeID == null)
                return 0;
            return (int)UserSession.EmployeeID;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string leaveType = LeaveTypeTextBox.Text.Trim();
            DateTime? startDate = StartDatePicker.SelectedDate;
            DateTime? endDate = EndDatePicker.SelectedDate;

            // 🔹 Validation
            if (string.IsNullOrEmpty(leaveType))
            {
                MessageBox.Show("Please enter a leave type.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (startDate == null)
            {
                MessageBox.Show("Please select a start date.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (endDate == null)
            {
                MessageBox.Show("Please select an end date.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (endDate < startDate)
            {
                MessageBox.Show("End date cannot be earlier than start date.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int employeeId = GetLoggedInEmployeeID();
            if (employeeId <= 0)
            {
                MessageBox.Show("Cannot determine your EmployeeID. Make sure your account is linked to an employee.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    if (EditingId.HasValue)
                    {
                        // 🔹 Update existing leave request
                        string updateQuery = @"UPDATE leaverequests
                                       SET LeaveType = @LeaveType,
                                           StartDate = @StartDate,
                                           EndDate = @EndDate,
                                           Status = @Status
                                       WHERE LeaveID = @Id";

                        using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@LeaveType", leaveType);
                            cmd.Parameters.AddWithValue("@StartDate", startDate.Value);
                            cmd.Parameters.AddWithValue("@EndDate", endDate.Value);
                            cmd.Parameters.AddWithValue("@Status", "Pending"); // ✅ Changed here
                            cmd.Parameters.AddWithValue("@Id", EditingId.Value);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Leave request updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // 🔹 Insert new leave request
                        string insertQuery = @"INSERT INTO leaverequests (EmployeeID, LeaveType, StartDate, EndDate, Status)
                                       VALUES (@EmployeeID, @LeaveType, @StartDate, @EndDate, @Status)";

                        using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                            cmd.Parameters.AddWithValue("@LeaveType", leaveType);
                            cmd.Parameters.AddWithValue("@StartDate", startDate.Value);
                            cmd.Parameters.AddWithValue("@EndDate", endDate.Value);
                            cmd.Parameters.AddWithValue("@Status", "Pending"); // ✅ Changed here
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Leave request submitted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                OnClose?.Invoke(true);
                CloseParentWindow();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving leave request: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            OnClose?.Invoke(false);
            CloseParentWindow();
        }

        private void CloseParentWindow()
        {
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
                parentWindow.Close();
        }
    }
}
