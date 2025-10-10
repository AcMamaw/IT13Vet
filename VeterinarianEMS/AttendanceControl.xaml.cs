using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using VeterinarianEMS.Models;

namespace VeterinarianEMS
{
    public partial class AttendanceControl : UserControl
    {
        private string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True;";

        // Master + filtered lists
        private List<AttendanceModel> _allAttendance = new();
        private List<AttendanceModel> _filteredAttendance = new();

        // Pagination fields
        private int currentPage = 1;
        private int pageSize = 5;   // ✅ default 5
        private int totalPages = 1;

        // Timer for live clock
        private DispatcherTimer _clockTimer;

        public AttendanceControl()
        {
            InitializeComponent();

            // Subscribe to EntriesPerPageSelector
            EntriesSelector.EntriesChanged += EntriesSelector_EntriesChanged;

            LoadAttendance();
            StartClock(); // ✅ Start live clock
        }

        // 🕒 Start live clock
        private void StartClock()
        {
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (s, e) =>
            {
                if (ClockTextBlock != null)
                {
                    ClockTextBlock.Text = DateTime.Now.ToString("hh:mm:ss tt - MMM dd, yyyy");
                }
            };
            _clockTimer.Start();
        }

        // 🔄 Load all attendance from DB
        private void LoadAttendance()
        {
            try
            {
                var tempList = new List<AttendanceModel>();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            a.AttendanceID,
                            a.EmployeeID,
                            e.FirstName,
                            e.MiddleName,
                            e.LastName,
                            a.Type,
                            a.DateTime,
                            s.StartTime,
                            s.EndTime
                        FROM attendance a
                        LEFT JOIN employees e ON a.EmployeeID = e.EmployeeID
                        LEFT JOIN employeeshifts es ON e.EmployeeID = es.EmployeeID
                        LEFT JOIN shifts s ON es.ShiftID = s.ShiftID
                        ORDER BY a.DateTime DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        int i = 1;
                        while (reader.Read())
                        {
                            string firstName = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            string middleName = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            string lastName = reader.IsDBNull(4) ? "" : reader.GetString(4);

                            string employeeName = firstName;
                            if (!string.IsNullOrWhiteSpace(middleName))
                                employeeName += " " + middleName[0] + ".";
                            if (!string.IsNullOrWhiteSpace(lastName))
                                employeeName += " " + lastName;

                            DateTime dateTime = reader.IsDBNull(6) ? DateTime.MinValue : reader.GetDateTime(6);
                            TimeSpan? shiftStart = reader.IsDBNull(7) ? null : reader.GetTimeSpan(7);

                            string status = "Present"; // default

                            // 🟡 Determine Status (same logic as EmpAttendanceControl)
                            if (reader["Type"].ToString() == "Sign In" && shiftStart != null)
                            {
                                DateTime shiftStartTime = dateTime.Date.Add(shiftStart.Value);

                                if (dateTime <= shiftStartTime.AddMinutes(10))
                                    status = "Present";
                                else if (dateTime > shiftStartTime.AddMinutes(10))
                                    status = "Late";
                            }

                            tempList.Add(new AttendanceModel
                            {
                                Number = i++,
                                AttendanceId = reader.GetInt32(0),
                                EmployeeId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                                EmployeeName = employeeName,
                                Type = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                Status = status,
                                DateTime = dateTime
                            });
                        }
                    }
                }

                _allAttendance = tempList;
                ApplySearchFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading attendance: " + ex.Message);
            }
        }

        // 🔍 Apply search filter
        private void ApplySearchFilter()
        {
            string keyword = SearchTextBox.Text?.ToLower() ?? "";

            _filteredAttendance = string.IsNullOrWhiteSpace(keyword)
                ? _allAttendance.ToList()
                : _allAttendance.Where(a =>
                        a.EmployeeId.ToString().Contains(keyword) ||
                        a.EmployeeName.ToLower().Contains(keyword) ||
                        a.Type.ToLower().Contains(keyword) ||
                        a.Status.ToLower().Contains(keyword) ||
                        a.DateTime.ToString("g").ToLower().Contains(keyword))
                    .ToList();

            currentPage = 1;
            LoadAttendancePage();
        }

        // 📄 Load current page
        private void LoadAttendancePage()
        {
            if (_filteredAttendance.Count == 0)
            {
                AttendanceDataGrid.ItemsSource = null;
                UpdatePageInfo();
                return;
            }

            totalPages = (int)Math.Ceiling((double)_filteredAttendance.Count / pageSize);
            currentPage = Math.Min(Math.Max(currentPage, 1), totalPages);

            var paged = _filteredAttendance
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            AttendanceDataGrid.ItemsSource = paged;
            UpdatePageInfo();
        }

        // 🔄 When entries per page changes
        private void EntriesSelector_EntriesChanged(object sender, int entries)
        {
            pageSize = entries;
            currentPage = 1;
            LoadAttendancePage();
        }

        // 🔍 Search text changed
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearchFilter();
        }

        // ⏮ Previous page
        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                LoadAttendancePage();
            }
        }

        // ⏭ Next page
        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage < totalPages)
            {
                currentPage++;
                LoadAttendancePage();
            }
        }

        // 🔄 Update footer info
        private void UpdatePageInfo()
        {
            if (PageNumberButton != null)
                PageNumberButton.Content = currentPage.ToString();

            if (PaginationStatus != null)
            {
                int total = _filteredAttendance.Count;
                int start = total == 0 ? 0 : ((currentPage - 1) * pageSize) + 1;
                int end = Math.Min(start + pageSize - 1, total);
                PaginationStatus.Text = $"Showing {currentPage} to {totalPages} of {total} entries";
            }
        }
    }
}
