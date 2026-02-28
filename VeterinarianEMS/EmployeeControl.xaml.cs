using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VeterinarianEMS.Controls;
using VeterinarianEMS.Views;
using static VeterinarianEMS.MainWindow;

namespace VeterinarianEMS
{
    public partial class EmployeeControl : UserControl
    {
        public event Action EmployeeSaved;
        private string connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True;";

        // Master list of employees (always holds everything from DB)
        private List<EmployeeViewModel> _allEmployees = new List<EmployeeViewModel>();

        // Filtered list (used for searching & paging)
        private List<EmployeeViewModel> _filteredEmployees = new List<EmployeeViewModel>();

        // Pagination fields
        private int currentPage = 1;
        private int pageSize = 5;   // ✅ default to 5
        private int totalPages = 1;

        public EmployeeControl()
        {
            InitializeComponent();

            // Subscribe to EntriesPerPageSelector event
            EntriesSelector.EntriesChanged += EntriesSelector_EntriesChanged;

            LoadEmployees();
        }

        // 🔄 Load employees from DB
        private void LoadEmployees()
        {
            try
            {
                var employeeDict = new Dictionary<int, EmployeeViewModel>();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
            SELECT e.EmployeeID, e.FirstName, e.MiddleName, e.LastName,
                   e.Sex, e.Age, e.ContactNumber, e.BaseSalary, e.HireDate,
                   e.DepartmentID, e.PositionID,
                   ISNULL(d.DepartmentName, 'No Dept') AS DepartmentName,
                   ISNULL(p.PositionName, 'No Position') AS PositionName,
                   s.StartTime, s.EndTime
            FROM dbo.employees e
            LEFT JOIN dbo.department d ON e.DepartmentID = d.DepartmentID
            LEFT JOIN dbo.empositions p ON e.PositionID = p.PositionID
            LEFT JOIN dbo.employeeshifts es ON e.EmployeeID = es.EmployeeID
            LEFT JOIN dbo.shifts s ON es.ShiftID = s.ShiftID;";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int empId = Convert.ToInt32(reader["EmployeeID"]);

                            if (!employeeDict.ContainsKey(empId))
                            {
                                string middleInitial = !string.IsNullOrEmpty(reader["MiddleName"]?.ToString())
                                    ? reader["MiddleName"].ToString()[0] + "."
                                    : "";

                                string fullName = $"{reader["FirstName"]} {middleInitial} {reader["LastName"]}".Trim();

                                employeeDict[empId] = new EmployeeViewModel
                                {
                                    Id = empId,
                                    Name = fullName,
                                    DepartmentID = reader["DepartmentID"] != DBNull.Value ? Convert.ToInt32(reader["DepartmentID"]) : 0,
                                    PositionID = reader["PositionID"] != DBNull.Value ? Convert.ToInt32(reader["PositionID"]) : 0,
                                    DepartmentName = reader["DepartmentName"].ToString(),
                                    PositionName = reader["PositionName"].ToString(),
                                    Shift = "", // will append below
                                    Sex = reader["Sex"]?.ToString() ?? "",
                                    Age = reader["Age"] != DBNull.Value ? Convert.ToInt32(reader["Age"]) : 0,
                                    Address = "N/A",
                                    ContactNumber = reader["ContactNumber"]?.ToString() ?? "",
                                    BaseSalary = reader["BaseSalary"] != DBNull.Value ? Convert.ToDecimal(reader["BaseSalary"]) : 0,
                                    HireDate = reader["HireDate"] != DBNull.Value ? Convert.ToDateTime(reader["HireDate"]) : (DateTime?)null
                                };
                            }

                            // Append StartTime - EndTime in 12-hour format with AM/PM
                            if (reader["StartTime"] != DBNull.Value && reader["EndTime"] != DBNull.Value)
                            {
                                TimeSpan startTs = (TimeSpan)reader["StartTime"];
                                TimeSpan endTs = (TimeSpan)reader["EndTime"];

                                // Convert to DateTime for 12-hour formatting
                                string startTime = DateTime.Today.Add(startTs).ToString("hh:mm tt");
                                string endTime = DateTime.Today.Add(endTs).ToString("hh:mm tt");

                                string shiftText = $"{startTime} - {endTime}";

                                if (!string.IsNullOrEmpty(employeeDict[empId].Shift))
                                {
                                    employeeDict[empId].Shift += ", ";
                                }
                                employeeDict[empId].Shift += shiftText;
                            }
                        }
                    }
                }

                // Reverse order: last employee first
                _allEmployees = employeeDict.Values
                                            .OrderByDescending(emp => emp.Id)
                                            .ToList();

                ApplySearchFilter(); // refresh view
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading employees:\n{ex.Message}",
                                "Database Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        // 🔎 Apply search filter without overwriting _allEmployees
        private void ApplySearchFilter()
        {
            string keyword = SearchTextBox.Text?.ToLower() ?? "";

            _filteredEmployees = string.IsNullOrWhiteSpace(keyword)
                ? _allEmployees.ToList()
                : _allEmployees.Where(emp =>
                        emp.Name.ToLower().Contains(keyword) ||
                        emp.DepartmentName.ToLower().Contains(keyword) ||
                        emp.PositionName.ToLower().Contains(keyword))
                    .ToList();

            currentPage = 1;
            LoadEmployeesPage();
        }

        // 📄 Load only the current page into the DataGrid
        private void LoadEmployeesPage()
        {
            if (_filteredEmployees.Count == 0)
            {
                EmployeeDataGrid.ItemsSource = null;
                UpdatePageInfo();
                return;
            }

            totalPages = (int)Math.Ceiling((double)_filteredEmployees.Count / pageSize);
            currentPage = Math.Min(Math.Max(currentPage, 1), totalPages);

            var pagedEmployees = _filteredEmployees
                                 .Skip((currentPage - 1) * pageSize)
                                 .Take(pageSize)
                                 .ToList();

            EmployeeDataGrid.ItemsSource = pagedEmployees;
            UpdatePageInfo();
        }

        // 🔄 Update page size when user changes EntriesPerPageSelector
        private void EntriesSelector_EntriesChanged(object sender, int entries)
        {
            pageSize = entries;
            currentPage = 1;
            LoadEmployeesPage();
        }

        // 🔍 Search box text changed
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearchFilter();
        }

        // ➕ Add Employee popup with fade-in
        private void AddEmployeeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var popupControl = new EmployeePopupControl();
                popupControl.EmployeeSaved += () => LoadEmployees(); // refresh employee list

                // Create hosting window
                var popupWindow = new Window
                {
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(this),
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.NoResize,
                    Background = System.Windows.Media.Brushes.Transparent,
                    AllowsTransparency = true,
                    ShowInTaskbar = false,
                    Content = popupControl,
                    Topmost = true,
                    Opacity = 0 // start transparent for fade-in
                };

                // Fade-in animation
                popupWindow.Loaded += (s, e2) =>
                {
                    var fade = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
                    popupWindow.BeginAnimation(Window.OpacityProperty, fade);
                };

                // Show modal
                popupWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while opening the Add Employee window:\n{ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeeDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Please select an employee to delete.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var employee = EmployeeDataGrid.SelectedItem as EmployeeViewModel;
            if (employee == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete {employee.Name}?\n" +
                "This will also delete related user and shift data.",
                "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Start a transaction for data safety
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 1️⃣ Delete related user record
                            string deleteUser = "DELETE FROM Users WHERE EmployeeID = @EmployeeID";
                            using (var cmdUser = new SqlCommand(deleteUser, connection, transaction))
                            {
                                cmdUser.Parameters.AddWithValue("@EmployeeID", employee.Id);
                                cmdUser.ExecuteNonQuery();
                            }

                            // 2️⃣ Delete related employee shifts (if applicable)
                            string deleteShifts = "DELETE FROM EmployeeShifts WHERE EmployeeID = @EmployeeID";
                            using (var cmdShift = new SqlCommand(deleteShifts, connection, transaction))
                            {
                                cmdShift.Parameters.AddWithValue("@EmployeeID", employee.Id);
                                cmdShift.ExecuteNonQuery();
                            }

                            // 3️⃣ Delete employee record
                            string deleteEmployee = "DELETE FROM Employees WHERE EmployeeID = @EmployeeID";
                            using (var cmdEmployee = new SqlCommand(deleteEmployee, connection, transaction))
                            {
                                cmdEmployee.Parameters.AddWithValue("@EmployeeID", employee.Id);
                                cmdEmployee.ExecuteNonQuery();
                            }

                            // ✅ Commit transaction if all succeed
                            transaction.Commit();

                            MessageBox.Show("Employee and related data deleted successfully.",
                                "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);

                            LoadEmployees(); // refresh datagrid
                        }
                        catch (Exception exInner)
                        {
                            // ❌ Rollback if any error occurs
                            transaction.Rollback();
                            MessageBox.Show("Error during deletion: " + exInner.Message,
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShiftButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (EmployeeDataGrid.SelectedItem is EmployeeViewModel employee)
                {
                    // Authorization check
                    if (UserSession.Role == null ||
                        !(UserSession.Role.Contains("HR", StringComparison.OrdinalIgnoreCase) ||
                          UserSession.Role.Contains("Human Resources", StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show("You are not authorized to manage employee shifts.",
                                        "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return; // Do not open the shift window
                    }

                    // Create shift control
                    var empShiftControl = new EmpShift
                    {
                        EmployeeID = employee.Id
                    };

                    // Subscribe to reload event
                    empShiftControl.ShiftSaved += () =>
                    {
                        LoadEmployees();
                    };

                    // Create hosting window
                    var shiftWindow = new Window
                    {
                        WindowStyle = WindowStyle.None,
                        AllowsTransparency = true,
                        Background = System.Windows.Media.Brushes.Transparent,
                        Content = empShiftControl,
                        SizeToContent = SizeToContent.WidthAndHeight,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.NoResize,
                        Topmost = true
                    };

                    // Fade-in animation
                    shiftWindow.Opacity = 0;
                    shiftWindow.Loaded += (s, e2) =>
                    {
                        var fade = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
                        shiftWindow.BeginAnimation(Window.OpacityProperty, fade);
                    };

                    // Show as modal
                    shiftWindow.ShowDialog();
                }
                else
                {
                    MessageBox.Show("Please select an employee first.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while opening the shift window:\n{ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeeDataGrid.SelectedItem is EmployeeViewModel selectedEmployee)
            {
                int employeeId = selectedEmployee.Id;

                var empView = new EmpView();
                empView.LoadEmployee(employeeId); // Load from DB

                var window = new Window
                {
                    Content = empView,
                    WindowStyle = WindowStyle.None,         // No title bar / X button
                    AllowsTransparency = true,              // Optional: makes it look like a panel
                    Background = Brushes.Transparent,       // Optional: show rounded border nicely
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ResizeMode = ResizeMode.NoResize,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Topmost = true
                };

                // Fade-in animation
                window.Opacity = 0;
                window.Loaded += (s, e2) =>
                {
                    var fade = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
                    window.BeginAnimation(Window.OpacityProperty, fade);
                };

                window.ShowDialog(); // modal popup
            }
            else
            {
                MessageBox.Show("Please select an employee first.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeeDataGrid.SelectedItem is EmployeeViewModel employee)
            {
                var editControl = new EmpEdit(employee.Id);

                editControl.EmployeeSaved += () =>
                {
                    LoadEmployees();
                };

                var window = new Window
                {
                    Content = editControl,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = Brushes.Transparent,
                    Width = 750,
                    Height = 700,
                    ResizeMode = ResizeMode.NoResize,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ShowInTaskbar = false,
                    Topmost = true
                };

                // Optional: Fade-in animation for smooth popup effect
                window.Opacity = 0;
                window.Loaded += (s, e2) =>
                {
                    var fade = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
                    window.BeginAnimation(Window.OpacityProperty, fade);
                };

                window.ShowDialog();
            }
            else
            {
                MessageBox.Show("Please select an employee first.",
                                "No Selection",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
            }
        }


        // 🟢 Selection changed (optional use: enable/disable buttons, etc.)
        private void EmployeeDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EmployeeDataGrid.SelectedItem is EmployeeViewModel selectedEmployee)
            {
                // You can enable Edit/Delete buttons here if needed
            }
            else
            {
                // No selection
            }
        }

        // ⏮ Previous page
        private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                LoadEmployeesPage();
            }
        }

        // ⏭ Next page
        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage < totalPages)
            {
                currentPage++;
                LoadEmployeesPage();
            }
        }

        // 🔄 Update Page Info text
        private void UpdatePageInfo()
        {
            // Show only the current page number in the middle button/textblock
            if (PageInfoTextBlock != null)
            {
                PageInfoTextBlock.Text = currentPage.ToString();
            }

            // Show the full "Page X of Y (Total: Z entries)" on the left side
            if (EntriesInfoTextBlock != null)
            {
                EntriesInfoTextBlock.Text = $"Show {currentPage} to {totalPages} of {_filteredEmployees.Count} entries";
            }
        }

    }
}
