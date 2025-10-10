using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace VeterinarianEMS
{
    public partial class OvertimeRequestControl : UserControl
    {
        public class OvertimeRequest
        {
            public int OvertimeID { get; set; }
            public string EmployeeName { get; set; }
            public DateTime OvertimeDate { get; set; }
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public string Status { get; set; }
        }

        private readonly string _connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True;";

        private List<OvertimeRequest> _allOvertimeRequests = new();
        private List<OvertimeRequest> _filteredOvertimeRequests = new();

        private int _currentPage = 1;
        private int _pageSize = 5;
        private int _totalPages = 1;

        private string _currentStatus = "Pending";

        public OvertimeRequestControl()
        {
            InitializeComponent();

            if (EntriesSelector != null)
                EntriesSelector.EntriesChanged += EntriesSelector_EntriesChanged;

            LoadOvertimeRequestsFromDatabase();
            UpdateStatus("Pending");
        }

        // 🔄 Load overtime requests from database
        private void LoadOvertimeRequestsFromDatabase()
        {
            _allOvertimeRequests.Clear();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT 
                            o.OvertimeID,
                            e.FirstName,
                            e.MiddleName,
                            e.LastName,
                            o.OvertimeDate,
                            o.StartTime,
                            o.EndTime,
                            o.Status
                        FROM OvertimeRequests o
                        INNER JOIN Employees e ON o.EmployeeID = e.EmployeeID
                        ORDER BY o.OvertimeDate DESC";

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

                            _allOvertimeRequests.Add(new OvertimeRequest
                            {
                                OvertimeID = reader.GetInt32(0),
                                EmployeeName = fullName,
                                OvertimeDate = reader.GetDateTime(4),
                                StartTime = reader.GetTimeSpan(5).ToString(@"hh\:mm"),
                                EndTime = reader.GetTimeSpan(6).ToString(@"hh\:mm"),
                                Status = reader.GetString(7)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading overtime requests: " + ex.Message,
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            UpdateCounts();
            FilterOvertimeRequests();
        }

        // 🔘 Update which status to show
        private void UpdateStatus(string status)
        {
            _currentStatus = status;
            _currentPage = 1;
            FilterOvertimeRequests();
        }

        // 🔍 Filter and search
        private void FilterOvertimeRequests()
        {
            string keyword = SearchBox.Text?.ToLower() ?? "";

            _filteredOvertimeRequests = _allOvertimeRequests
                .Where(o => o.Status.Equals(_currentStatus, StringComparison.OrdinalIgnoreCase)
                            && (string.IsNullOrEmpty(keyword)
                                || o.EmployeeName.ToLower().Contains(keyword)))
                .ToList();

            UpdateCounts();
            LoadPage();
        }

        // 🔢 Update status counts
        private void UpdateCounts()
        {
            PendingCount.Text = _allOvertimeRequests.Count(o => o.Status == "Pending").ToString();
            ApprovedCount.Text = _allOvertimeRequests.Count(o => o.Status == "Approved").ToString();
            RejectedCount.Text = _allOvertimeRequests.Count(o => o.Status == "Rejected").ToString();
        }

        // 📄 Load page
        private void LoadPage()
        {
            if (_filteredOvertimeRequests.Count == 0)
            {
                OvertimeDataGrid.ItemsSource = null;
                UpdatePageInfo();
                return;
            }

            _totalPages = (int)Math.Ceiling((double)_filteredOvertimeRequests.Count / _pageSize);
            _currentPage = Math.Min(Math.Max(_currentPage, 1), _totalPages);

            var paged = _filteredOvertimeRequests
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

            OvertimeDataGrid.ItemsSource = paged;
            UpdatePageInfo();
        }

        // 🧾 Pagination footer
        private void UpdatePageInfo()
        {
            int total = _filteredOvertimeRequests.Count;
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
            FilterOvertimeRequests();
        }

        // 🔢 Entries per page
        private void EntriesSelector_EntriesChanged(object sender, int entries)
        {
            _pageSize = entries;
            _currentPage = 1;
            LoadPage();
        }

        // ✅ Approve overtime
        private void ApproveOvertime_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is OvertimeRequest overtime)
                UpdateOvertimeStatus(overtime.OvertimeID, "Approved");
        }

        // ❌ Reject overtime
        private void RejectOvertime_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is OvertimeRequest overtime)
                UpdateOvertimeStatus(overtime.OvertimeID, "Rejected");
        }

        // 🗑️ Delete overtime
        private void DeleteOvertime_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is OvertimeRequest overtime)
            {
                var confirm = MessageBox.Show(
                    $"Are you sure you want to delete overtime request ID {overtime.OvertimeID}?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(_connectionString))
                        {
                            conn.Open();
                            string query = "DELETE FROM OvertimeRequests WHERE OvertimeID = @OvertimeID";
                            using (SqlCommand cmd = new SqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@OvertimeID", overtime.OvertimeID);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        _allOvertimeRequests.Remove(overtime);
                        FilterOvertimeRequests();

                        MessageBox.Show("Overtime request deleted successfully.",
                            "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error deleting overtime request: " + ex.Message,
                            "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // 🔄 Update overtime status
        private void UpdateOvertimeStatus(int overtimeID, string newStatus)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = "UPDATE OvertimeRequests SET Status = @Status WHERE OvertimeID = @OvertimeID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Status", newStatus);
                        cmd.Parameters.AddWithValue("@OvertimeID", overtimeID);
                        cmd.ExecuteNonQuery();
                    }
                }

                var overtime = _allOvertimeRequests.FirstOrDefault(o => o.OvertimeID == overtimeID);
                if (overtime != null)
                    overtime.Status = newStatus;

                MessageBox.Show($"Overtime request {overtimeID} marked as {newStatus}.",
                    "Status Updated", MessageBoxButton.OK, MessageBoxImage.Information);

                FilterOvertimeRequests();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating overtime status: " + ex.Message,
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
