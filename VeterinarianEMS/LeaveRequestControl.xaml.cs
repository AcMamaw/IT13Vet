using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace VeterinarianEMS
{
    public partial class LeaveRequestControl : UserControl
    {
        public class LeaveRequest
        {
            public int LeaveID { get; set; }
            public string EmployeeName { get; set; }
            public string LeaveType { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Status { get; set; }
        }

        private readonly string _connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True;";

        private List<LeaveRequest> _allLeaveRequests = new();
        private List<LeaveRequest> _filteredLeaveRequests = new();

        private int _currentPage = 1;
        private int _pageSize = 5;
        private int _totalPages = 1;

        private string _currentStatus = "Pending";

        public LeaveRequestControl()
        {
            InitializeComponent();

            if (EntriesSelector != null)
                EntriesSelector.EntriesChanged += EntriesSelector_EntriesChanged;

            LoadLeaveRequestsFromDatabase();
            UpdateStatus("Pending");
        }

        // 🔄 Load leave requests from database
        private void LoadLeaveRequestsFromDatabase()
        {
            _allLeaveRequests.Clear();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT 
                            l.LeaveID,
                            e.FirstName,
                            e.MiddleName,
                            e.LastName,
                            l.LeaveType,
                            l.StartDate,
                            l.EndDate,
                            l.Status
                        FROM LeaveRequests l
                        INNER JOIN Employees e ON l.EmployeeID = e.EmployeeID
                        ORDER BY l.StartDate DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string first = reader.GetString(1);
                            string middle = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            string last = reader.GetString(3);

                            string fullName = first;
                            if (!string.IsNullOrWhiteSpace(middle))
                                fullName += " " + middle[0] + ".";
                            fullName += " " + last;

                            _allLeaveRequests.Add(new LeaveRequest
                            {
                                LeaveID = reader.GetInt32(0),
                                EmployeeName = fullName,
                                LeaveType = reader.GetString(4),
                                StartDate = reader.GetDateTime(5),
                                EndDate = reader.GetDateTime(6),
                                Status = reader.GetString(7)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading leave requests: " + ex.Message,
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            UpdateCounts();
            FilterLeaveRequests();
        }

        // 🔘 Update which status to show
        private void UpdateStatus(string status)
        {
            _currentStatus = status;
            _currentPage = 1;
            FilterLeaveRequests();
        }

        // 🔍 Filter and search
        private void FilterLeaveRequests()
        {
            string keyword = SearchBox.Text?.ToLower() ?? "";

            _filteredLeaveRequests = _allLeaveRequests
                .Where(l => l.Status.Equals(_currentStatus, StringComparison.OrdinalIgnoreCase)
                            && (string.IsNullOrEmpty(keyword)
                                || l.EmployeeName.ToLower().Contains(keyword)
                                || l.LeaveType.ToLower().Contains(keyword)))
                .ToList();

            UpdateCounts();
            LoadPage();
        }

        // 🔢 Update status counts
        private void UpdateCounts()
        {
            PendingCount.Text = _allLeaveRequests.Count(l => l.Status == "Pending").ToString();
            ApprovedCount.Text = _allLeaveRequests.Count(l => l.Status == "Approved").ToString();
            RejectedCount.Text = _allLeaveRequests.Count(l => l.Status == "Rejected").ToString();
        }

        // 📄 Load page
        private void LoadPage()
        {
            if (_filteredLeaveRequests.Count == 0)
            {
                LeaveDataGrid.ItemsSource = null;
                UpdatePageInfo();
                return;
            }

            _totalPages = (int)Math.Ceiling((double)_filteredLeaveRequests.Count / _pageSize);
            _currentPage = Math.Min(Math.Max(_currentPage, 1), _totalPages);

            var paged = _filteredLeaveRequests
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

            LeaveDataGrid.ItemsSource = paged;
            UpdatePageInfo();
        }

        // 🧾 Pagination footer
        private void UpdatePageInfo()
        {
            int total = _filteredLeaveRequests.Count;
            int start = total == 0 ? 0 : ((_currentPage - 1) * _pageSize) + 1;
            int end = Math.Min(start + _pageSize - 1, total);
            ShowingText.Text = $"Showing {_currentPage} to {_totalPages} of {total} entries";
            PageInfoTextBlock.Text = $"{_currentPage}";
        }

        // 🔘 Status filter buttons
        private void PendingButton_Click(object sender, RoutedEventArgs e) => UpdateStatus("Pending");
        private void ApprovedButton_Click(object sender, RoutedEventArgs e) => UpdateStatus("Approved");
        private void RejectedButton_Click(object sender, RoutedEventArgs e) => UpdateStatus("Rejected");

        // ⏮ Pagination buttons
        private void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadPage();
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadPage();
            }
        }

        // 🔍 Search
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentPage = 1;
            FilterLeaveRequests();
        }

        // 🔢 Entries per page
        private void EntriesSelector_EntriesChanged(object sender, int entries)
        {
            _pageSize = entries;
            _currentPage = 1;
            LoadPage();
        }

        // ✅ Approve leave
        private void ApproveLeave_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is LeaveRequest leave)
                UpdateLeaveStatus(leave.LeaveID, "Approved");
        }

        // ❌ Reject leave
        private void RejectLeave_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is LeaveRequest leave)
                UpdateLeaveStatus(leave.LeaveID, "Rejected");
        }

        // 🗑️ Delete leave
        private void DeleteLeave_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is LeaveRequest leave)
            {
                var confirm = MessageBox.Show(
                    $"Are you sure you want to delete leave request ID {leave.LeaveID}?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(_connectionString))
                        {
                            conn.Open();
                            string query = "DELETE FROM LeaveRequests WHERE LeaveID = @LeaveID";
                            using (SqlCommand cmd = new SqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@LeaveID", leave.LeaveID);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        _allLeaveRequests.Remove(leave);
                        FilterLeaveRequests();

                        MessageBox.Show("Leave request deleted successfully.",
                            "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error deleting leave request: " + ex.Message,
                            "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // 🔄 Update leave status
        private void UpdateLeaveStatus(int leaveID, string newStatus)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = "UPDATE LeaveRequests SET Status = @Status WHERE LeaveID = @LeaveID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Status", newStatus);
                        cmd.Parameters.AddWithValue("@LeaveID", leaveID);
                        cmd.ExecuteNonQuery();
                    }
                }

                var leave = _allLeaveRequests.FirstOrDefault(l => l.LeaveID == leaveID);
                if (leave != null)
                    leave.Status = newStatus;

                MessageBox.Show($"Leave request {leaveID} marked as {newStatus}.",
                    "Status Updated", MessageBoxButton.OK, MessageBoxImage.Information);

                FilterLeaveRequests();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating leave status: " + ex.Message,
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
