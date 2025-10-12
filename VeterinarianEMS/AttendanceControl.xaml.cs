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

     // 🔄 Load all attendance (for all employees, with normalized Type + leave/absent logic)
private void LoadAttendance()
{
    try
    {
        var tempList = new List<AttendanceModel>();

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();

            string query = @"
                WITH AttendanceStatus AS (
                    SELECT 
                        e.EmployeeID,
                        (e.FirstName + ' ' + e.LastName) AS EmployeeName,
                        a.Type AS RawType,
                        a.DateTime,
                        CAST(a.DateTime AS DATE) AS AttendanceDate,
                        s.StartTime
                    FROM attendance a
                    INNER JOIN employees e ON a.EmployeeID = e.EmployeeID
                    LEFT JOIN employeeshifts es ON e.EmployeeID = es.EmployeeID
                    LEFT JOIN shifts s ON es.ShiftID = s.ShiftID
                ),
                LeaveCheck AS (
                    SELECT 
                        l.EmployeeID,
                        lb.LeaveType,
                        lb.RemainingLeaves,
                        l.StartDate,
                        l.EndDate
                    FROM leaverequests l
                    INNER JOIN leavebalance lb ON l.EmployeeID = lb.EmployeeID
                    WHERE l.Status = 'Approved'
                ),
                -- normalize sign/type and evaluate Late vs IN using StartTime
                Normalized AS (
                    SELECT
                        a.EmployeeID,
                        a.EmployeeName,
                        a.RawType,
                        a.DateTime,
                        a.AttendanceDate,
                        a.StartTime,
                        CASE
                            -- Sign In (DB may store 'Sign In' or 'IN') -> determine IN vs Late by comparing to shift start
                            WHEN a.RawType IN ('Sign In', 'IN') AND a.StartTime IS NOT NULL THEN
                                CASE
                                    WHEN a.DateTime <= DATEADD(MINUTE, 10, DATEADD(SECOND, DATEDIFF(SECOND, 0, a.StartTime), CAST(a.AttendanceDate AS DATETIME))) THEN 'IN'
                                    ELSE 'Late'
                                END
                            WHEN a.RawType IN ('Sign Out', 'OUT') THEN 'OUT'
                            ELSE NULL
                        END AS CalcType
                    FROM AttendanceStatus a
                )

                SELECT 
                    ROW_NUMBER() OVER (ORDER BY COALESCE(n.DateTime, GETDATE()) DESC) AS Number,
                    e.EmployeeID,
                    (e.FirstName + ' ' + e.LastName) AS EmployeeName,
                    -- final Type: normalized if attendance exists, otherwise leave info or --
                    CASE
                        WHEN n.CalcType IS NOT NULL THEN n.CalcType
                    WHEN l.EmployeeID IS NOT NULL THEN 
                        '' + CAST(l.RemainingLeaves AS VARCHAR(10)) + ' Remain Days'
                    ELSE '--'
                    END AS Type,
                    -- Status: Present if normalized type exists (IN/Late/OUT), On Leave if leave exists, otherwise Absent
                    CASE
                        WHEN n.CalcType IN ('IN', 'Late', 'OUT') THEN 'Present'
                        WHEN l.EmployeeID IS NOT NULL THEN 'On Leave'
                        ELSE 'Absent'
                    END AS Status,
                    COALESCE(n.DateTime, GETDATE()) AS DateTime
                FROM employees e
                LEFT JOIN Normalized n ON e.EmployeeID = n.EmployeeID
                LEFT JOIN LeaveCheck l 
                    ON e.EmployeeID = l.EmployeeID
                    AND CAST(GETDATE() AS DATE) BETWEEN l.StartDate AND l.EndDate
                ORDER BY COALESCE(n.DateTime, GETDATE()) DESC;";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                int i = 1;
                while (reader.Read())
                {
                    tempList.Add(new AttendanceModel
                    {
                        Number = i++,
                        EmployeeId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                        EmployeeName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        Type = reader.IsDBNull(3) ? "--" : reader.GetString(3),
                        Status = reader.IsDBNull(4) ? "Absent" : reader.GetString(4),
                        DateTime = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5)
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
