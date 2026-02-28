using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using static VeterinarianEMS.MainWindow;

namespace VeterinarianEMS
{
    public partial class EmpAttendanceControl : UserControl
    {
        private readonly string _connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True;";

        private int? _employeeId => UserSession.EmployeeID;
        private DispatcherTimer _clockTimer;

        // 🌟 Pagination Fields
        private int currentPage = 1;
        private int pageSize = 5; // default 5 entries per page
        private int totalPages = 1;

        private DataTable _allAttendanceTable;
        private DataView _filteredAttendanceView;

        public EmpAttendanceControl()
        {
            InitializeComponent();
            StartClock();           // 🕒 Start live clock
            LoadAttendance();       // 📋 Load attendance table
            MarkAbsentIfNoSignIn(); // 🚫 Mark absent if no sign-in
            EntriesSelector.EntriesChanged += EntriesSelector_EntriesChanged;
        }

        // 🕒 CLOCK FUNCTION
        private void StartClock()
        {
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (s, e) =>
            {
                ClockTextBlock.Text = DateTime.Now.ToString("hh:mm:ss tt - MMM dd, yyyy");
            };
            _clockTimer.Start();
        }
        // 🔄 Load attendance for logged-in employee
        private void LoadAttendance()
        {
            if (_employeeId == null)
            {
                MessageBox.Show("Employee ID not found for logged-in user.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"
WITH AttendanceStatus AS (
    SELECT 
        e.EmployeeID,
        (e.FirstName + ' ' + e.LastName) AS EmployeeName,
        a.Type,
        a.Status,
        a.DateTime
    FROM attendance a
    INNER JOIN employees e ON a.EmployeeID = e.EmployeeID
    WHERE e.EmployeeID = @empId
),
LeaveCheck AS (
    SELECT 
        l.EmployeeID,
        lb.LeaveType,
        lb.RemainingLeaves
    FROM leaverequests l
    INNER JOIN leavebalance lb ON l.EmployeeID = lb.EmployeeID
    WHERE l.Status = 'Approved'
      AND CAST(GETDATE() AS DATE) BETWEEN l.StartDate AND l.EndDate
),
ShiftCheck AS (
    SELECT 
        es.EmployeeID,
        es.ShiftDays
    FROM employeeshifts es
    WHERE es.EmployeeID = @empId
)
SELECT 
    ROW_NUMBER() OVER (ORDER BY COALESCE(a.DateTime, GETDATE()) DESC) AS Number,
    e.EmployeeID,
    e.FirstName + ' ' + e.LastName AS EmployeeName,
    CASE 
        WHEN a.Status = 'Absent' THEN '--'
        WHEN a.Type IN ('IN', 'OUT', 'Late') THEN a.Type
        WHEN l.EmployeeID IS NOT NULL THEN 'Leave (' + l.LeaveType + ' - ' + CAST(l.RemainingLeaves AS VARCHAR) + ')'
        ELSE '--'
    END AS Type,
    CASE 
        WHEN a.Status = 'Present' THEN 'Present'
        WHEN l.EmployeeID IS NOT NULL THEN 'On Leave'
        ELSE 'Absent'
    END AS Status,
    -- ✅ Show date in MM/dd/yyyy format + 12-hour time
    CASE 
        WHEN a.DateTime IS NOT NULL THEN FORMAT(a.DateTime, 'MM/dd/yyyy hh:mm tt')
        ELSE '--'
    END AS DateTime,
    s.ShiftDays
FROM employees e
LEFT JOIN AttendanceStatus a ON e.EmployeeID = a.EmployeeID
LEFT JOIN LeaveCheck l ON e.EmployeeID = l.EmployeeID
LEFT JOIN ShiftCheck s ON e.EmployeeID = s.EmployeeID
WHERE e.EmployeeID = @empId
ORDER BY COALESCE(a.DateTime, GETDATE()) DESC;
";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@empId", _employeeId);

                        SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        _allAttendanceTable = dt;
                        _filteredAttendanceView = dt.DefaultView;

                        ApplySearchFilter(); // apply pagination + search
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading attendance: " + ex.Message);
            }
        }


        // 🔍 APPLY SEARCH FILTER
        private void ApplySearchFilter()
        {
            string keyword = SearchTextBox.Text?.ToLower() ?? "";
            if (_allAttendanceTable == null) return;

            if (string.IsNullOrWhiteSpace(keyword))
                _filteredAttendanceView.RowFilter = "";
            else
                _filteredAttendanceView.RowFilter =
                    $"Convert(EmployeeId, 'System.String') LIKE '%{keyword}%' OR " +
                    $"EmployeeName LIKE '%{keyword}%' OR Type LIKE '%{keyword}%' OR Status LIKE '%{keyword}%'";

            currentPage = 1;
            LoadAttendancePage();
        }

        // 📄 LOAD CURRENT PAGE
        private void LoadAttendancePage()
        {
            if (_filteredAttendanceView == null || _filteredAttendanceView.Count == 0)
            {
                AttendanceDataGrid.ItemsSource = null;
                UpdatePageInfo();
                return;
            }

            totalPages = (int)Math.Ceiling((double)_filteredAttendanceView.Count / pageSize);
            currentPage = Math.Min(Math.Max(currentPage, 1), totalPages);

            // paginate
            var pagedTable = _filteredAttendanceView.ToTable().AsEnumerable()
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .CopyToDataTable();

            AttendanceDataGrid.ItemsSource = pagedTable.DefaultView;
            UpdatePageInfo();
        }

        // ⏮ PREVIOUS PAGE
        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                LoadAttendancePage();
            }
        }

        // ⏭ NEXT PAGE
        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage < totalPages)
            {
                currentPage++;
                LoadAttendancePage();
            }
        }

        // 🔄 UPDATE FOOTER INFO
        private void UpdatePageInfo()
        {
            if (PaginationStatus != null)
            {
                int total = _filteredAttendanceView?.Count ?? 0;
                int start = total == 0 ? 0 : ((currentPage - 1) * pageSize) + 1;
                int end = Math.Min(start + pageSize - 1, total);
                PaginationStatus.Text = $"Showing {currentPage} to {totalPages} of {total} entries";
                PageNumberButton.Content = currentPage.ToString(); 

            }
        }

        // 🔍 SEARCH TEXT CHANGED
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearchFilter();
        }

        // 🟢 SIGN IN
        private void OnInButtonClick(object sender, RoutedEventArgs e)
        {
            RecordAttendance("Sign In");
        }

        // 🔴 SIGN OUT
        private void OnOutButtonClick(object sender, RoutedEventArgs e)
        {
            RecordAttendance("Sign Out");
        }


        private void EntriesSelector_EntriesChanged(object sender, int entries)
        {
            pageSize = entries;   // update page size
            currentPage = 1;      // reset to first page
            LoadAttendancePage(); // refresh DataGrid
        }


        // 💾 RECORD ATTENDANCE (IN/OUT)
        private void RecordAttendance(string type)
        {
            if (_employeeId == null)
            {
                MessageBox.Show("Employee not recognized.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // ❌ Prevent if already Absent
                    string checkAbsentQuery = @"
                        SELECT COUNT(*) FROM attendance 
                        WHERE EmployeeID = @empId 
                        AND CONVERT(date, DateTime) = CONVERT(date, GETDATE()) 
                        AND Status = 'Absent'";

                    SqlCommand absentCheckCmd = new SqlCommand(checkAbsentQuery, conn);
                    absentCheckCmd.Parameters.AddWithValue("@empId", _employeeId);

                    int absentCount = (int)absentCheckCmd.ExecuteScalar();
                    if (absentCount > 0)
                    {
                        MessageBox.Show("You are already marked as Absent today. You cannot sign in or out.", "Access Denied",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 🕗 Check if already signed in today
                    string checkSignInQuery = @"
                        SELECT COUNT(*) FROM attendance 
                        WHERE EmployeeID = @empId 
                        AND CONVERT(date, DateTime) = CONVERT(date, GETDATE())
                        AND Type = 'Sign In'";

                    SqlCommand checkSignInCmd = new SqlCommand(checkSignInQuery, conn);
                    checkSignInCmd.Parameters.AddWithValue("@empId", _employeeId);
                    int alreadySignedIn = (int)checkSignInCmd.ExecuteScalar();

                    if (type == "Sign In" && alreadySignedIn > 0)
                    {
                        MessageBox.Show("You already signed in today.", "Duplicate Entry",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // ⏰ Get shift
                    string shiftQuery = @"
                        SELECT TOP 1 s.ShiftID, s.StartTime, s.EndTime
                        FROM employeeshifts es
                        INNER JOIN shifts s ON es.ShiftID = s.ShiftID
                        WHERE es.EmployeeID = @empId";

                    SqlCommand shiftCmd = new SqlCommand(shiftQuery, conn);
                    shiftCmd.Parameters.AddWithValue("@empId", _employeeId);

                    int? shiftId = null;
                    TimeSpan? startTime = null;
                    using (SqlDataReader reader = shiftCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            shiftId = reader.GetInt32(reader.GetOrdinal("ShiftID"));
                            startTime = reader.GetTimeSpan(reader.GetOrdinal("StartTime"));
                        }
                    }

                    if (shiftId == null)
                    {
                        MessageBox.Show("No shift assigned to this employee.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // ✅ Determine status
                    DateTime now = DateTime.Now;
                    DateTime shiftStart = DateTime.Today.Add(startTime.Value);
                    string status = now <= shiftStart.AddMinutes(10) ? "Present" : "Late";

                    // 🧾 Insert record
                    string insertQuery = @"
                        INSERT INTO attendance (EmployeeID, ShiftID, Type, Status, DateTime)
                        VALUES (@empId, @shiftId, @type, @status, @datetime)";

                    SqlCommand insertCmd = new SqlCommand(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@empId", _employeeId);
                    insertCmd.Parameters.AddWithValue("@shiftId", shiftId);
                    insertCmd.Parameters.AddWithValue("@type", type);
                    insertCmd.Parameters.AddWithValue("@status", status);
                    insertCmd.Parameters.AddWithValue("@datetime", now);
                    insertCmd.ExecuteNonQuery();

                    MessageBox.Show($"{type} recorded successfully ({status})!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadAttendance();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error recording attendance: " + ex.Message);
            }
        }

        // 🚫 AUTO MARK ABSENT
        private void MarkAbsentIfNoSignIn()
        {
            if (_employeeId == null) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT TOP 1 s.ShiftID, s.StartTime
                        FROM employeeshifts es
                        INNER JOIN shifts s ON es.ShiftID = s.ShiftID
                        WHERE es.EmployeeID = @empId";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@empId", _employeeId);

                    int? shiftId = null;
                    TimeSpan? startTime = null;
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            shiftId = reader.GetInt32(reader.GetOrdinal("ShiftID"));
                            startTime = reader.GetTimeSpan(reader.GetOrdinal("StartTime"));
                        }
                    }

                    if (shiftId == null) return;

                    DateTime shiftStart = DateTime.Today.Add(startTime.Value);
                    if (DateTime.Now < shiftStart.AddHours(2)) return;

                    string checkQuery = @"
                        SELECT COUNT(*) FROM attendance
                        WHERE EmployeeID = @empId
                        AND CONVERT(date, DateTime) = CONVERT(date, GETDATE())
                        AND Type = 'Sign In'";

                    SqlCommand checkCmd = new SqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@empId", _employeeId);

                    int count = (int)checkCmd.ExecuteScalar();
                    if (count == 0)
                    {
                        string absentQuery = @"
                            INSERT INTO attendance (EmployeeID, ShiftID, Type, Status, DateTime)
                            VALUES (@empId, @shiftId, 'Sign In', 'Absent', GETDATE())";

                        SqlCommand absentCmd = new SqlCommand(absentQuery, conn);
                        absentCmd.Parameters.AddWithValue("@empId", _employeeId);
                        absentCmd.Parameters.AddWithValue("@shiftId", shiftId);
                        absentCmd.ExecuteNonQuery();

                        LoadAttendance();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error marking absence: " + ex.Message);
            }
        }
    }
}
