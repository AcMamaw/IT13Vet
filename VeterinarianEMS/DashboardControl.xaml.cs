using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace VeterinarianEMS.Controls
{
    public partial class DashboardControl : UserControl
    {
        private readonly string connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True;";

        public DashboardControl()
        {
            InitializeComponent();
            LoadDashboardData();
            LoadTopInfoCards();
        }

        private void LoadDashboardData()
        {
            LoadTopAttendance();
            LoadTopOvertime();
            LoadTopRatedEmployees();
            LoadPendingAndOnLeave();
            LoadAttendanceTrend();
            LoadEmployeeSatisfaction();
        }

        // ✅ Models for binding to ListBoxes
        public class TopAttendanceItem
        {
            public string EmployeeName { get; set; }
            public int DaysPresent { get; set; }
        }

        public class TopOvertimeItem
        {
            public string EmployeeName { get; set; }
            public int OvertimeHours { get; set; }
        }

        // ✅ Top Info Cards
        private void LoadTopInfoCards()
        {
            int pendingLeave = 0;
            int pendingOvertime = 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Pending Leaves
                SqlCommand leaveCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM leaverequests WHERE Status = 'Pending'", conn);
                pendingLeave = (int)leaveCmd.ExecuteScalar();

                // Pending Overtime
                SqlCommand overtimeCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM overtimerequests WHERE Status = 'Pending'", conn);
                pendingOvertime = (int)overtimeCmd.ExecuteScalar();
            }

            PendingLeaveTextBlock.Text = $"{pendingLeave} Pending";
            PendingOvertimeTextBlock.Text = $"{pendingOvertime} Pending";

            DateTime today = DateTime.Today;
            DateTime nextPayroll = new DateTime(today.Year, today.Month, 31);
            int daysUntilPayroll = (nextPayroll - today).Days;
            DaysUntilPayrollTextBlock.Text = daysUntilPayroll.ToString();
        }

        // ✅ Top Attendance Loader
        private void LoadTopAttendance()
        {
            var list = new List<TopAttendanceItem>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT EmployeeName, DaysPresent FROM vw_TopAttendance", conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new TopAttendanceItem
                    {
                        EmployeeName = reader["EmployeeName"].ToString(),
                        DaysPresent = Convert.ToInt32(reader["DaysPresent"])
                    });
                }
            }

            TopAttendanceList.ItemsSource = list;
        }

        // ✅ FIXED - Use correct column name OvertimeHours
        private void LoadTopOvertime()
        {
            var list = new List<TopOvertimeItem>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT EmployeeName, OvertimeHours FROM vw_TopOvertime", conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new TopOvertimeItem
                    {
                        EmployeeName = reader["EmployeeName"].ToString(),
                        OvertimeHours = Convert.ToInt32(reader["OvertimeHours"])
                    });
                }
            }

            TopOvertimeList.ItemsSource = list;
        }

        // ✅ Keep the rest unchanged
        private void LoadTopRatedEmployees()
        {
            var items = new List<string>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT EmployeeName, AvgRating FROM vw_TopRatedEmployees", conn);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    items.Add($"{reader["EmployeeName"]} - {Convert.ToDouble(reader["AvgRating"]):0.0}");
                }
            }
            TopRatedEmployeesList.ItemsSource = items;
        }

        private void LoadPendingAndOnLeave()
        {
            int onTimeCount = 0;
            int lateCount = 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                SqlCommand pendingCmd = new SqlCommand("SELECT SUM(PendingLeaveCount) FROM vw_PendingLeaves", conn);
                var pending = pendingCmd.ExecuteScalar();
                PendingLeavesText.Text = pending?.ToString() ?? "0";

                SqlCommand onLeaveCmd = new SqlCommand("SELECT COUNT(*) FROM vw_EmployeesOnLeaveToday", conn);
                var onLeave = onLeaveCmd.ExecuteScalar();
                EmployeesOnLeaveText.Text = onLeave?.ToString() ?? "0";

                string attendanceQuery = @"
                    SELECT a.EmployeeID, a.DateTime AS AttendanceTime, s.StartTime
                    FROM attendance a
                    INNER JOIN shifts s ON a.ShiftID = s.ShiftID
                    WHERE CAST(a.DateTime AS DATE) = CAST(GETDATE() AS DATE)
                      AND a.Type = 'IN'
                ";

                using (SqlCommand cmd = new SqlCommand(attendanceQuery, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DateTime attendanceTime = Convert.ToDateTime(reader["AttendanceTime"]);
                        TimeSpan shiftStart = TimeSpan.Parse(reader["StartTime"].ToString());

                        if (attendanceTime.TimeOfDay <= shiftStart)
                            onTimeCount++;
                        else
                            lateCount++;
                    }
                }
            }

            OnTimeEmployeesText.Text = onTimeCount.ToString();
            LateEmployeesText.Text = lateCount.ToString();
        }

        private void LoadAttendanceTrend()
        {
            var months = new List<string>();
            var values = new ChartValues<int>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT MonthName, DaysPresent FROM vw_AttendanceTrend ORDER BY Year, MonthNumber", conn);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    months.Add(reader["MonthName"].ToString());
                    values.Add(Convert.ToInt32(reader["DaysPresent"]));
                }
            }

            AttendanceTrendChart.Series = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Attendance",
                    Values = values,
                    Fill = new SolidColorBrush(Color.FromRgb(155, 89, 182))
                }
            };

            AttendanceTrendChart.AxisX.Clear();
            AttendanceTrendChart.AxisX.Add(new LiveCharts.Wpf.Axis
            {
                Title = "Month",
                Labels = months
            });

            AttendanceTrendChart.AxisY.Clear();
            AttendanceTrendChart.AxisY.Add(new LiveCharts.Wpf.Axis
            {
                Title = "Days Present",
                LabelFormatter = value => value.ToString()
            });
        }

        private void LoadEmployeeSatisfaction()
        {
            var series = new SeriesCollection();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT FeedbackType, Count FROM vw_EmployeeSatisfaction", conn);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var type = reader["FeedbackType"].ToString();
                    var count = Convert.ToDouble(reader["Count"]);

                    Brush color = Brushes.Gray;
                    if (type.Equals("Positive", StringComparison.OrdinalIgnoreCase))
                        color = Brushes.Green;
                    else if (type.Equals("Neutral", StringComparison.OrdinalIgnoreCase))
                        color = Brushes.Yellow;
                    else if (type.Equals("Negative", StringComparison.OrdinalIgnoreCase))
                        color = Brushes.Red;

                    series.Add(new PieSeries
                    {
                        Title = type,
                        Values = new ChartValues<double> { count },
                        Fill = color
                    });
                }
            }

            EmployeeSatisfactionChart.Series = series;
        }
    }
}
