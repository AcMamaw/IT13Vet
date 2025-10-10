using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VeterinarianEMS.Controls;
using static VeterinarianEMS.MainWindow;

namespace VeterinarianEMS
{
    public class OvertimeRequestModel
    {
        public int OvertimeID { get; set; }
        public int EmployeeID { get; set; }

        public string EmployeeName { get; set; }
        public DateTime OvertimeDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Status { get; set; }
    }

    public partial class EmpOvertimeRequestControl : UserControl
    {
        private readonly string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True;";

        private List<OvertimeRequestModel> _allOvertimes = new();
        private List<OvertimeRequestModel> _filteredOvertimes = new();

        private int currentPage = 1;
        private int pageSize = 5;
        private int totalPages = 1;

        public EmpOvertimeRequestControl()
        {
            InitializeComponent();
            Loaded += EmpOvertimeRequestControl_Loaded;
        }

        private void EmpOvertimeRequestControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadOvertimeRequests();
        }

        private void LoadOvertimeRequests()
        {
            try
            {
                var overtimeList = new List<OvertimeRequestModel>();
                int? employeeId = UserSession.EmployeeID;

                if (employeeId == null)
                {
                    MessageBox.Show("No employee is currently logged in.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // 🔹 Get the employee’s full name first
                    string employeeName = "";
                    using (SqlCommand nameCmd = new SqlCommand(
                        "SELECT FirstName + ' ' + LastName FROM employees WHERE EmployeeID = @EmployeeID", conn))
                    {
                        nameCmd.Parameters.AddWithValue("@EmployeeID", employeeId.Value);
                        object result = nameCmd.ExecuteScalar();
                        if (result != null)
                            employeeName = result.ToString();
                    }

                    // 🔹 Get that employee’s overtime requests
                    string query = @"
                SELECT o.OvertimeID, o.EmployeeID,
                       o.OvertimeDate, o.StartTime, o.EndTime, o.Status
                FROM overtimerequests o
                WHERE o.EmployeeID = @EmployeeID
                ORDER BY o.OvertimeID DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeID", employeeId.Value);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string startTime = reader.IsDBNull(3)
                                    ? ""
                                    : ((TimeSpan)reader["StartTime"]).ToString(@"hh\:mm");

                                string endTime = reader.IsDBNull(4)
                                    ? ""
                                    : ((TimeSpan)reader["EndTime"]).ToString(@"hh\:mm");

                                overtimeList.Add(new OvertimeRequestModel
                                {
                                    OvertimeID = reader.GetInt32(0),
                                    EmployeeID = reader.GetInt32(1),
                                    EmployeeName = employeeName, // ✅ show full name instead of ID
                                    OvertimeDate = reader.GetDateTime(2),
                                    StartTime = startTime,
                                    EndTime = endTime,
                                    Status = reader.IsDBNull(5) ? "" : reader.GetString(5)
                                });
                            }
                        }
                    }
                }

                _allOvertimes = overtimeList;
                ApplySearchFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading overtime requests: " + ex.Message,
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔹 SEARCH
        private void ApplySearchFilter()
        {
            string keyword = SearchTextBox?.Text?.Trim().ToLower() ?? "";

            _filteredOvertimes = string.IsNullOrWhiteSpace(keyword)
                ? _allOvertimes.ToList()
                : _allOvertimes.Where(x =>
                        x.EmployeeName.ToLower().Contains(keyword) ||
                        x.OvertimeDate.ToString("d").Contains(keyword) ||
                        x.StartTime.ToLower().Contains(keyword) ||
                        x.EndTime.ToLower().Contains(keyword) ||
                        x.Status.ToLower().Contains(keyword))
                    .ToList();

            currentPage = 1;
            LoadOvertimePage();
        }

        // 🔹 PAGINATION
        private void LoadOvertimePage()
        {
            if (_filteredOvertimes.Count == 0)
            {
                OvertimeDataGrid.ItemsSource = null;
                UpdatePageInfo();
                return;
            }

            totalPages = (int)Math.Ceiling((double)_filteredOvertimes.Count / pageSize);
            currentPage = Math.Min(Math.Max(currentPage, 1), totalPages);

            var paged = _filteredOvertimes
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            OvertimeDataGrid.ItemsSource = paged;
            UpdatePageInfo();
        }

        private void UpdatePageInfo()
        {
            if (PageInfoTextBlock != null)
                PageInfoTextBlock.Text = $"{currentPage}";

            int start = _filteredOvertimes.Count == 0 ? 0 : ((currentPage - 1) * pageSize) + 1;
            int end = _filteredOvertimes.Count == 0 ? 0 : Math.Min(start + pageSize - 1, _filteredOvertimes.Count);

            if (EntriesInfoTextBlock != null)
                EntriesInfoTextBlock.Text = $"Showing {currentPage} to {totalPages} of {_filteredOvertimes.Count} entries";
        }

        // 🔹 EVENT HANDLERS
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearchFilter();
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                LoadOvertimePage();
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage < totalPages)
            {
                currentPage++;
                LoadOvertimePage();
            }
        }
        private void AddOvertime_Click(object sender, RoutedEventArgs e)
        {
            var popup = new OvertimeRequestPopup();

            // 🔹 Reload overtime list after saving
            popup.OnSaved += () =>
            {
                LoadOvertimeRequests();
            };

            Window popupWindow = new Window
            {
                Width = 400,
                Height = 450,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = popup,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                Owner = Window.GetWindow(this),
                Background = System.Windows.Media.Brushes.Transparent,
                AllowsTransparency = true
            };

            popupWindow.ShowDialog();
        }

        private void DeleteOvertime_Click(object sender, RoutedEventArgs e)
        {
            if (OvertimeDataGrid?.SelectedItem is OvertimeRequestModel selected)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"Are you sure you want to delete this overtime request?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            conn.Open();
                            string deleteQuery = "DELETE FROM overtimerequests WHERE OvertimeID = @OvertimeID";
                            using (SqlCommand cmd = new SqlCommand(deleteQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@OvertimeID", selected.OvertimeID);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        LoadOvertimeRequests();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error deleting overtime request: " + ex.Message,
                            "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
