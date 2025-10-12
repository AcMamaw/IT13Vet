using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Windows.Xps;
using System.Printing;
using System.Windows.Documents;


namespace VeterinarianEMS
{
    public partial class PayrollControl : UserControl
    {
        private readonly string _connString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True";
        private bool _isComboBoxInitialized = false;

        private List<string> _allEmployees = new List<string>();
        private List<PayrollRecord> _allPayrolls = new List<PayrollRecord>();
        private List<PayrollRecord> _filteredPayrolls = new List<PayrollRecord>();

        // Pagination
        private int currentPage = 1;
        private int pageSize = 5;
        private int totalPages = 1;

        public PayrollControl()
        {
            InitializeComponent();
            LoadEmployees();
            LoadPayrolls();
        }

        #region Load Data
        private void LoadEmployees()
        {
            _allEmployees.Clear();
            _allEmployees.Add("All Employees"); // dropdown option

            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    string query = @"
                SELECT e.EmployeeID, e.FirstName, e.MiddleName, e.LastName, 
                       p.PositionName
                FROM employees e
                LEFT JOIN empositions p ON e.PositionID = p.PositionID
                WHERE p.PositionName NOT IN ('HR Staff', 'Human Resources')
                ORDER BY e.FirstName, e.LastName;";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string fullName = reader["FirstName"].ToString();
                            if (reader["MiddleName"] != DBNull.Value && !string.IsNullOrEmpty(reader["MiddleName"].ToString()))
                                fullName += " " + reader["MiddleName"];
                            fullName += " " + reader["LastName"];
                            _allEmployees.Add(fullName);
                        }
                    }
                }

                // Bind to ComboBox
                EmployeeComboBox.ItemsSource = _allEmployees;

                // Keep placeholder text in the editable field
                EmployeeComboBox.Text = "Search Employee";
                EmployeeComboBox.Foreground = Brushes.Gray;

                _isComboBoxInitialized = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading employees: " + ex.Message);
            }
        }
        private void LoadPayrolls()
        {
            _allPayrolls.Clear();
            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    string query = @"
                SELECT p.PayrollID, p.EmployeeID, p.PayPeriodStart, p.PayPeriodEnd,
                       p.TotalHoursWorked, p.LeaveDays, p.ExportDate, p.ComputedSalary,
                       e.FirstName, e.MiddleName, e.LastName
                FROM payrollexport p
                LEFT JOIN employees e ON p.EmployeeID = e.EmployeeID
                ORDER BY p.PayrollID DESC"; // last inserted first

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string fullName = reader["FirstName"] != DBNull.Value
                                ? reader["FirstName"].ToString() +
                                  (reader["MiddleName"] != DBNull.Value && !string.IsNullOrEmpty(reader["MiddleName"].ToString()) ? " " + reader["MiddleName"] : "") +
                                  " " + reader["LastName"]
                                : "Unknown";

                            _allPayrolls.Add(new PayrollRecord
                            {
                                PayrollID = Convert.ToInt32(reader["PayrollID"]),
                                EmployeeID = reader["EmployeeID"] != DBNull.Value ? Convert.ToInt32(reader["EmployeeID"]) : 0,
                                EmployeeName = fullName,
                                PayPeriodStart = reader["PayPeriodStart"] != DBNull.Value ? Convert.ToDateTime(reader["PayPeriodStart"]) : (DateTime?)null,
                                PayPeriodEnd = reader["PayPeriodEnd"] != DBNull.Value ? Convert.ToDateTime(reader["PayPeriodEnd"]) : (DateTime?)null,
                                TotalHoursWorked = reader["TotalHoursWorked"] != DBNull.Value ? Convert.ToInt32(reader["TotalHoursWorked"]) : 0,
                                LeaveDays = reader["LeaveDays"] != DBNull.Value ? Convert.ToInt32(reader["LeaveDays"]) : 0,
                                ExportDate = reader["ExportDate"] != DBNull.Value ? Convert.ToDateTime(reader["ExportDate"]) : (DateTime?)null,
                                ComputedSalary = reader["ComputedSalary"] != DBNull.Value ? Convert.ToDecimal(reader["ComputedSalary"]) : 0
                            });
                        }
                    }
                }

                ApplySearchFilter(); // refresh view
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading payrolls: " + ex.Message);
            }
        }

        #endregion

        #region Search / Filter

        private void ApplySearchFilter()
        {
            string keyword = SearchBox.Text?.ToLower() ?? "";

            _filteredPayrolls = string.IsNullOrWhiteSpace(keyword)
                ? _allPayrolls.ToList()
                : _allPayrolls.Where(p => p.EmployeeName.ToLower().Contains(keyword)).ToList();

            currentPage = 1;
            LoadPayrollPage();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearchFilter();
        }

        #endregion

        #region ComboBox Filtering (Dropdown only)

        private void EmployeeComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (!_isComboBoxInitialized) return;

            string text = EmployeeComboBox.Text.ToLower();
            var filtered = _allEmployees.Where(emp => emp.ToLower().Contains(text)).ToList();
            EmployeeComboBox.ItemsSource = filtered;
            EmployeeComboBox.IsDropDownOpen = true;

            if (EmployeeComboBox.Template.FindName("PART_EditableTextBox", EmployeeComboBox) is TextBox textBox)
            {
                textBox.Text = EmployeeComboBox.Text;
                textBox.CaretIndex = textBox.Text.Length;
            }
        }

        private void EmployeeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // ComboBox selection does NOT affect grid
        }

        #endregion

        #region Pagination

        private void LoadPayrollPage()
        {
            if (_filteredPayrolls.Count == 0)
            {
                PayrollDataGrid.ItemsSource = null;
                UpdatePageInfo();
                return;
            }

            totalPages = (int)Math.Ceiling((double)_filteredPayrolls.Count / pageSize);
            currentPage = Math.Min(Math.Max(currentPage, 1), totalPages);

            var paged = _filteredPayrolls
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            PayrollDataGrid.ItemsSource = paged;
            UpdatePageInfo();
        }

        private void EmployeeComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (EmployeeComboBox.Text == "Search Employee")
            {
                EmployeeComboBox.Text = "";
                EmployeeComboBox.Foreground = Brushes.Black;
            }
        }

        private void EmployeeComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EmployeeComboBox.Text))
            {
                EmployeeComboBox.Text = "Search Employee";
                EmployeeComboBox.Foreground = Brushes.Gray;
            }
        }


        private void UpdatePageInfo()
        {
            if (_filteredPayrolls.Count == 0)
            {
                PaginationStatus.Text = "Showing 0 to 0 of 0 entries";
                CurrentPageTextBlock.Text = "0";
                PreviousButton.IsEnabled = false;
                NextButton.IsEnabled = false;
                return;
            }

            int start = (currentPage - 1) * pageSize + 1;
            int end = Math.Min(currentPage * pageSize, _filteredPayrolls.Count);

            // Update status text
            PaginationStatus.Text = $"Showing {currentPage} to {totalPages} of {_filteredPayrolls.Count} entries";
            CurrentPageTextBlock.Text = currentPage.ToString();

            // Enable/disable buttons
            PreviousButton.IsEnabled = currentPage > 1;
            NextButton.IsEnabled = currentPage < totalPages;
        }

        private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                LoadPayrollPage();
            }
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage < totalPages)
            {
                currentPage++;
                LoadPayrollPage();
            }
        }
        
        private void EntriesSelector_EntriesChanged(object sender, int entries)
        {
            pageSize = entries;
            currentPage = 1;
            LoadPayrollPage();
        }


        #endregion

        #region Payroll Generation

        private void GeneratePayroll_Click(object sender, RoutedEventArgs e)
        {
            var start = MyDateRange.StartDate;
            var end = MyDateRange.EndDate;

            if (start == null || end == null)
            {
                MessageBox.Show("Please select a valid date range.");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();

                    List<int> employeeIds = new List<int>();
                    string selectedEmployee = EmployeeComboBox.Text.Trim();

                    string empQuery;

                    if (string.IsNullOrEmpty(selectedEmployee) || selectedEmployee == "All Employees")
                    {
                        empQuery = @"
                    SELECT e.EmployeeID
                    FROM employees e
                    LEFT JOIN empositions p ON e.PositionID = p.PositionID
                    WHERE p.PositionName NOT IN ('HR Staff', 'Human Resources')";
                    }
                    else
                    {
                        empQuery = @"
                    SELECT e.EmployeeID
                    FROM employees e
                    LEFT JOIN empositions p ON e.PositionID = p.PositionID
                    WHERE p.PositionName NOT IN ('HR Staff', 'Human Resources')
                    AND CONCAT(e.FirstName, ' ', ISNULL(e.MiddleName,''), ' ', e.LastName) LIKE @Name";
                    }

                    using (SqlCommand cmd = new SqlCommand(empQuery, conn))
                    {
                        if (!(string.IsNullOrEmpty(selectedEmployee) || selectedEmployee == "All Employees"))
                            cmd.Parameters.AddWithValue("@Name", "%" + selectedEmployee + "%");

                        using (SqlDataReader reader = cmd.ExecuteReader())
                            while (reader.Read())
                                employeeIds.Add(Convert.ToInt32(reader["EmployeeID"]));
                    }

                    if (employeeIds.Count == 0)
                    {
                        MessageBox.Show("No valid employees found (HR Staff are excluded).");
                        return;
                    }

                    foreach (int empId in employeeIds)
                    {
                        // --- Attendance ---
                        string attQuery = @"
                    SELECT DateTime, Type 
                    FROM attendance
                    WHERE EmployeeID = @EmpID 
                      AND DateTime BETWEEN @Start AND @End
                      AND Type IN ('IN','OUT')
                    ORDER BY DateTime";

                        List<DateTime> attendanceTimes = new List<DateTime>();
                        using (SqlCommand cmd = new SqlCommand(attQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@EmpID", empId);
                            cmd.Parameters.AddWithValue("@Start", start.Value.Date);
                            cmd.Parameters.AddWithValue("@End", end.Value.Date.AddDays(1).AddSeconds(-1));

                            using (SqlDataReader reader = cmd.ExecuteReader())
                                while (reader.Read())
                                    attendanceTimes.Add(Convert.ToDateTime(reader["DateTime"]));
                        }

                        int totalHours = 0;
                        for (int i = 0; i < attendanceTimes.Count; i += 2)
                        {
                            if (i + 1 < attendanceTimes.Count)
                                totalHours += (int)(attendanceTimes[i + 1] - attendanceTimes[i]).TotalHours;
                        }

                        // --- Leave Days ---
                        string leaveQuery = @"
                    SELECT StartDate, EndDate
                    FROM leaverequests
                    WHERE EmployeeID = @EmpID
                      AND Status = 'Approved'
                      AND EndDate >= @Start
                      AND StartDate <= @End";

                        int leaveDays = 0;
                        using (SqlCommand cmd = new SqlCommand(leaveQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@EmpID", empId);
                            cmd.Parameters.AddWithValue("@Start", start.Value.Date);
                            cmd.Parameters.AddWithValue("@End", end.Value.Date);

                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    DateTime leaveStart = Convert.ToDateTime(reader["StartDate"]);
                                    DateTime leaveEnd = Convert.ToDateTime(reader["EndDate"]);

                                    // Calculate overlap with payroll period
                                    DateTime actualStart = leaveStart < start.Value.Date ? start.Value.Date : leaveStart;
                                    DateTime actualEnd = leaveEnd > end.Value.Date ? end.Value.Date : leaveEnd;

                                    leaveDays += (actualEnd - actualStart).Days + 1; // inclusive
                                }
                            }
                        }

                        // --- Hourly rate ---
                        decimal hourlyRate = 0;
                        string rateQuery = "SELECT HourlyRate FROM employees WHERE EmployeeID=@EmpID";
                        using (SqlCommand cmd = new SqlCommand(rateQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@EmpID", empId);
                            using (SqlDataReader reader = cmd.ExecuteReader())
                                if (reader.Read())
                                    hourlyRate = reader["HourlyRate"] != DBNull.Value ? Convert.ToDecimal(reader["HourlyRate"]) : 0;
                        }

                        decimal computedSalary = hourlyRate * totalHours;

                        // --- Insert payroll ---
                        string insertQuery = @"
                    INSERT INTO payrollexport 
                    (EmployeeID, PayPeriodStart, PayPeriodEnd, TotalHoursWorked, LeaveDays, ComputedSalary, ExportDate)
                    VALUES (@EmpID, @Start, @End, @Hours, @Leave, @ComputedSalary, @ExportDate)";

                        using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@EmpID", empId);
                            cmd.Parameters.AddWithValue("@Start", start.Value.Date);
                            cmd.Parameters.AddWithValue("@End", end.Value.Date);
                            cmd.Parameters.AddWithValue("@Hours", totalHours);
                            cmd.Parameters.AddWithValue("@Leave", leaveDays);
                            cmd.Parameters.AddWithValue("@ComputedSalary", computedSalary);
                            cmd.Parameters.AddWithValue("@ExportDate", DateTime.Now);
                            cmd.ExecuteNonQuery();
                        }

                        Console.WriteLine($"✅ Employee {empId}: Hours={totalHours}, LeaveDays={leaveDays}, Rate={hourlyRate}, ComputedSalary={computedSalary}");
                    }
                }

                MessageBox.Show("Payroll generated successfully!");
                LoadPayrolls();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generating payroll: " + ex.Message);
            }
        }

        #endregion

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1️⃣ Create FlowDocument
                FlowDocument doc = new FlowDocument
                {
                    PagePadding = new Thickness(50),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 12,
                    ColumnWidth = double.PositiveInfinity
                };

                // 2️⃣ Add a header
                Paragraph header = new Paragraph(new Run("Payroll Report"))
                {
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20)
                };
                doc.Blocks.Add(header);

                // 3️⃣ Create table
                Table table = new Table();
                doc.Blocks.Add(table);

                // Add columns
                int columnCount = 6; // Employee, Start, End, Hours, Leave, Salary
                for (int i = 0; i < columnCount; i++)
                    table.Columns.Add(new TableColumn());

                // Add header row
                TableRowGroup headerGroup = new TableRowGroup();
                TableRow headerRow = new TableRow();
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Employee"))) { FontWeight = FontWeights.Bold });
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Start"))) { FontWeight = FontWeights.Bold });
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("End"))) { FontWeight = FontWeights.Bold });
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Hours"))) { FontWeight = FontWeights.Bold });
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Leave"))) { FontWeight = FontWeights.Bold });
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Salary"))) { FontWeight = FontWeights.Bold });
                headerGroup.Rows.Add(headerRow);
                table.RowGroups.Add(headerGroup);

                // Add data rows
                TableRowGroup dataGroup = new TableRowGroup();
                foreach (var payroll in _filteredPayrolls) // or _allPayrolls
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run(payroll.EmployeeName))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(payroll.PayPeriodStart?.ToShortDateString() ?? ""))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(payroll.PayPeriodEnd?.ToShortDateString() ?? ""))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(payroll.TotalHoursWorked.ToString()))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(payroll.LeaveDays.ToString()))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(payroll.ComputedSalary.ToString("C"))))); // currency
                    dataGroup.Rows.Add(row);
                }
                table.RowGroups.Add(dataGroup);

                // 4️⃣ Print to PDF
                LocalPrintServer printServer = new LocalPrintServer();
                PrintQueue pdfQueue = printServer.GetPrintQueue("Microsoft Print to PDF");
                string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "PayrollReport.pdf");
                PrintTicket ticket = pdfQueue.DefaultPrintTicket;
                ticket.PageOrientation = PageOrientation.Portrait;
                XpsDocumentWriter writer = PrintQueue.CreateXpsDocumentWriter(pdfQueue);

                using (var stream = new FileStream(outputPath, FileMode.Create))
                {
                    IDocumentPaginatorSource idpSource = doc;
                    writer.Write(idpSource.DocumentPaginator, ticket);
                }

                MessageBox.Show($"Payroll Report successfully generated at:\n{outputPath}", "Print Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generating PDF:\n" + ex.Message, "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public class PayrollRecord
        {
            public int PayrollID { get; set; }
            public int EmployeeID { get; set; }
            public string EmployeeName { get; set; }
            public DateTime? PayPeriodStart { get; set; }
            public DateTime? PayPeriodEnd { get; set; }
            public int TotalHoursWorked { get; set; }
            public int LeaveDays { get; set; }
            public DateTime? ExportDate { get; set; }
            public decimal ComputedSalary { get; set; }

        }
    }
}
