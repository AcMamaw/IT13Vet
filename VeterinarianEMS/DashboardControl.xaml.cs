using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
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

        #region Models

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

        public class TopRatedEmployeeItem
        {
            public string EmployeeName { get; set; }
            public double Rating { get; set; }
        }

        #endregion

        #region Top Info Cards

        private void LoadTopInfoCards()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    int pendingLeave = Convert.ToInt32(
                        new SqlCommand("SELECT COUNT(*) FROM leaverequests WHERE Status = 'Pending'", conn)
                        .ExecuteScalar());

                    int pendingOvertime = Convert.ToInt32(
                        new SqlCommand("SELECT COUNT(*) FROM overtimerequests WHERE Status = 'Pending'", conn)
                        .ExecuteScalar());

                    PendingLeaveTextBlock.Text = $"{pendingLeave} Pending";
                    PendingOvertimeTextBlock.Text = $"{pendingOvertime} Pending";

                    // FIXED Payroll Date (no crash on Feb, Apr, etc.)
                    DateTime today = DateTime.Today;
                    DateTime nextPayroll = new DateTime(today.Year, today.Month,
                        DateTime.DaysInMonth(today.Year, today.Month));

                    if (today > nextPayroll)
                        nextPayroll = nextPayroll.AddMonths(1);

                    DaysUntilPayrollTextBlock.Text =
                        (nextPayroll - today).Days.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard cards:\n{ex.Message}");
            }
        }

        #endregion

        #region Top Attendance

        private void LoadTopAttendance()
        {
            var list = new List<TopAttendanceItem>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT EmployeeName, DaysPresent FROM vw_TopAttendance", conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new TopAttendanceItem
                            {
                                EmployeeName = reader["EmployeeName"]?.ToString(),
                                DaysPresent = Convert.ToInt32(reader["DaysPresent"])
                            });
                        }
                    }
                }

                TopAttendanceList.ItemsSource = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading top attendance:\n{ex.Message}");
            }
        }

        #endregion

        #region Top Overtime

        private void LoadTopOvertime()
        {
            var list = new List<TopOvertimeItem>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT EmployeeName, OvertimeHours FROM vw_TopOvertime", conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new TopOvertimeItem
                            {
                                EmployeeName = reader["EmployeeName"]?.ToString(),
                                OvertimeHours = Convert.ToInt32(reader["OvertimeHours"])
                            });
                        }
                    }
                }

                TopOvertimeList.ItemsSource = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading overtime:\n{ex.Message}");
            }
        }

        #endregion

        #region Top Rated Employees

        private void LoadTopRatedEmployees()
        {
            var items = new List<TopRatedEmployeeItem>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT TOP 3 FirstName, MiddleName, LastName, EmployeeRating
                        FROM TopRatedEmployees
                        WHERE EmployeeRating IS NOT NULL
                        ORDER BY EmployeeRating DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string fullName =
                                $"{reader["FirstName"]} {reader["MiddleName"]} {reader["LastName"]}"
                                .Replace("  ", " ")
                                .Trim();

                            items.Add(new TopRatedEmployeeItem
                            {
                                EmployeeName = fullName,
                                Rating = Convert.ToDouble(reader["EmployeeRating"])
                            });
                        }
                    }
                }

                TopRatedEmployeesList.ItemsSource = items;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading top rated employees:\n{ex.Message}");
            }
        }

        #endregion

        #region Pending & On Leave

        private void LoadPendingAndOnLeave()
        {
            int onTimeCount = 0;
            int lateCount = 0;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    PendingLeavesText.Text = Convert.ToString(
                        new SqlCommand("SELECT SUM(PendingLeaveCount) FROM vw_PendingLeaves", conn)
                        .ExecuteScalar()) ?? "0";

                    EmployeesOnLeaveText.Text = Convert.ToString(
                        new SqlCommand("SELECT COUNT(*) FROM vw_EmployeesOnLeaveToday", conn)
                        .ExecuteScalar()) ?? "0";

                    string query = @"
                        SELECT a.DateTime, s.StartTime
                        FROM attendance a
                        INNER JOIN shifts s ON a.ShiftID = s.ShiftID
                        WHERE CAST(a.DateTime AS DATE) = CAST(GETDATE() AS DATE)
                        AND a.Type = 'IN'";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DateTime attendanceTime =
                                Convert.ToDateTime(reader["DateTime"]);

                            TimeSpan shiftStart =
                                TimeSpan.Parse(reader["StartTime"].ToString());

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
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading attendance summary:\n{ex.Message}");
            }
        }

        #endregion

        #region Attendance Trend Chart

        private void LoadAttendanceTrend()
        {
            var months = new List<string>();
            var values = new ChartValues<int>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT MonthName, DaysPresent FROM vw_AttendanceTrend ORDER BY Year, MonthNumber", conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            months.Add(reader["MonthName"].ToString());
                            values.Add(Convert.ToInt32(reader["DaysPresent"]));
                        }
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
                AttendanceTrendChart.AxisX.Add(new Axis
                {
                    Title = "Month",
                    Labels = months
                });

                AttendanceTrendChart.AxisY.Clear();
                AttendanceTrendChart.AxisY.Add(new Axis
                {
                    Title = "Days Present"
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading attendance chart:\n{ex.Message}");
            }
        }

        #endregion

        #region Employee Satisfaction Chart

        private void LoadEmployeeSatisfaction()
        {
            var series = new SeriesCollection();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT FeedbackType, Count FROM vw_EmployeeSatisfaction", conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string type = reader["FeedbackType"].ToString();
                            double count = Convert.ToDouble(reader["Count"]);

                            Brush color = Brushes.Gray;
                            if (type.Equals("Positive", StringComparison.OrdinalIgnoreCase))
                                color = Brushes.Green;
                            else if (type.Equals("Neutral", StringComparison.OrdinalIgnoreCase))
                                color = Brushes.Gold;
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
                }

                EmployeeSatisfactionChart.Series = series;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading satisfaction chart:\n{ex.Message}");
            }
        }

        #endregion
    }
}