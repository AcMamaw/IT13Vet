using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VeterinarianEMS.Controls;
using static VeterinarianEMS.MainWindow;

namespace VeterinarianEMS
{
    // LeaveRequestModel class inside the same namespace
    public class LeaveRequestModel
    {
        public int LeaveID { get; set; }
        public int EmployeeID { get; set; }
        public string LeaveType { get; set; }
        public string EmployeeName { get; set; } 

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
    }

    public partial class EmpLeaveRequestControl : UserControl
    {
        private readonly string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True;";

        private List<LeaveRequestModel> _allLeaves = new List<LeaveRequestModel>();
        private List<LeaveRequestModel> _filteredLeaves = new List<LeaveRequestModel>();

        private int currentPage = 1;
        private int pageSize = 5;
        private int totalPages = 1;

        private int employeeId = 0;

        public EmpLeaveRequestControl()
        {
            InitializeComponent();

            Loaded += EmpLeaveRequestControl_Loaded;
        }

        private void EmpLeaveRequestControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Replace this with your session logic
            employeeId = 1; // For testing, set a default employee ID

            LoadLeaveRequests();
        }
        private void LoadLeaveRequests()
        {
            try
            {
                int? employeeId = UserSession.EmployeeID;

                if (employeeId == null)
                {
                    MessageBox.Show("No employee is currently logged in.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var leaves = new List<LeaveRequestModel>();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                SELECT l.LeaveID, l.EmployeeID, 
                       (e.FirstName + ' ' + e.LastName) AS EmployeeName,
                       l.LeaveType, l.StartDate, l.EndDate, l.Status
                FROM leaverequests l
                INNER JOIN employees e ON l.EmployeeID = e.EmployeeID
                WHERE l.EmployeeID = @EmployeeID
                ORDER BY l.LeaveID DESC"; // DESC so newest appears first

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeID", employeeId.Value);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                leaves.Add(new LeaveRequestModel
                                {
                                    LeaveID = reader.GetInt32(0),
                                    EmployeeID = reader.GetInt32(1),
                                    EmployeeName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    LeaveType = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                    StartDate = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4),
                                    EndDate = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5),
                                    Status = reader.IsDBNull(6) ? "" : reader.GetString(6)
                                });
                            }
                        }
                    }
                }

                _allLeaves = leaves;
                ApplySearchFilter(); // ✅ Keep your filtering logic
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading leave requests: " + ex.Message,
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ApplySearchFilter()
        {
            string keyword = SearchTextBox?.Text?.ToLower() ?? "";

            _filteredLeaves = string.IsNullOrWhiteSpace(keyword)
                ? _allLeaves.ToList()
                : _allLeaves.Where(x =>
                        x.LeaveID.ToString().Contains(keyword) ||
                        x.LeaveType.ToLower().Contains(keyword) ||
                        x.StartDate.ToString("d").Contains(keyword) ||
                        x.EndDate.ToString("d").Contains(keyword) ||
                        x.Status.ToLower().Contains(keyword))
                    .ToList();

            currentPage = 1;
            LoadLeavesPage();
        }

        private void LoadLeavesPage()
        {
            if (_filteredLeaves.Count == 0)
            {
                if (LeaveDataGrid != null)
                    LeaveDataGrid.ItemsSource = null;
                UpdatePageInfo();
                return;
            }

            totalPages = (int)Math.Ceiling((double)_filteredLeaves.Count / pageSize);
            currentPage = Math.Min(Math.Max(currentPage, 1), totalPages);

            var pagedLeaves = _filteredLeaves
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            if (LeaveDataGrid != null)
                LeaveDataGrid.ItemsSource = pagedLeaves;

            UpdatePageInfo();
        }

        private void UpdatePageInfo()
        {
            if (PageInfoTextBlock != null)
                PageInfoTextBlock.Text = $"{currentPage}";

            int start = _filteredLeaves.Count == 0 ? 0 : ((currentPage - 1) * pageSize) + 1;
            int end = _filteredLeaves.Count == 0 ? 0 : Math.Min(start + pageSize - 1, _filteredLeaves.Count);

            if (EntriesInfoTextBlock != null)
                EntriesInfoTextBlock.Text = $"Showing {currentPage} to {totalPages} of {_filteredLeaves.Count} entries";
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearchFilter();
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                LoadLeavesPage();
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage < totalPages)
            {
                currentPage++;
                LoadLeavesPage();
            }
        }

        private void AddLeave_Click(object sender, RoutedEventArgs e)
        {
            // Create a new LeaveRequestPopup
            var leavePopup = new LeaveRequestPopup();

            // Subscribe to OnClose event to refresh the table after the popup closes
            leavePopup.OnClose += (success) =>
            {
                if (success)
                {
                    LoadLeaveRequests(); // 🔹 Refresh the table
                }
            };

            // Create a new Window to host the popup
            Window popupWindow = new Window
            {
                Width = 400,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = leavePopup, // use the instance with the event subscribed
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                Owner = Window.GetWindow(this),
                Background = System.Windows.Media.Brushes.Transparent,
                AllowsTransparency = true
            };

            popupWindow.ShowDialog(); // Opens the popup modally
        }


        private void EditLeave_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Edit Leave Popup placeholder. Implement LeaveRequestPopup separately.");
        }

        private void DeleteLeave_Click(object sender, RoutedEventArgs e)
        {
            if (LeaveDataGrid?.SelectedItem is LeaveRequestModel selectedLeave)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"Are you sure you want to delete this leave request?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            conn.Open();
                            string deleteQuery = "DELETE FROM leaverequests WHERE LeaveID = @LeaveID";
                            using (SqlCommand cmd = new SqlCommand(deleteQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@LeaveID", selectedLeave.LeaveID);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        LoadLeaveRequests();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error deleting leave request: " + ex.Message);
                    }
                }
            }
        }
    }
}
