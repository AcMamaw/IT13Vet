using Microsoft.Data.SqlClient;
using System;
using System.Windows;
using System.Windows.Controls;
using static VeterinarianEMS.MainWindow;

namespace VeterinarianEMS.Views
{
    public partial class EmpEdit : UserControl
    {
        private readonly int _employeeId;
        private readonly string _connString = @"Data Source=(localdb)\MSSQLLocalDB;
                                                Initial Catalog=VeterinarianEMS;
                                                Integrated Security=True;
                                                Connect Timeout=30;
                                                Encrypt=False;
                                                Trust Server Certificate=False;
                                                Application Intent=ReadWrite;
                                                Multi Subnet Failover=False";

        // ✅ Event to notify parent to refresh employees
        public event Action EmployeeSaved;

        public EmpEdit(int employeeId)
        {
            InitializeComponent();
            _employeeId = employeeId;

            LoadDepartments();
            LoadPositions();
            LoadEmployee(_employeeId);
        }

        // Load employee data from DB and pre-fill fields
        private void LoadEmployee(int employeeId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    string query = @"SELECT EmployeeID, FirstName, MiddleName, LastName, Sex, PositionID, DepartmentID,
                                            ContactNumber, BaseSalary, HireDate, Address, DOB
                                     FROM employees
                                     WHERE EmployeeID = @EmployeeID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeID", employeeId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                FirstNameTextBox.Text = reader["FirstName"].ToString();
                                MiddleNameTextBox.Text = reader["MiddleName"].ToString();
                                LastNameTextBox.Text = reader["LastName"].ToString();

                                string sex = reader["Sex"].ToString();
                                SexComboBox.SelectedIndex = sex == "Male" ? 0 : sex == "Female" ? 1 : -1;

                                DOBPicker.SelectedDate = reader["DOB"] as DateTime?;
                                ContactTextBox.Text = reader["ContactNumber"].ToString();
                                HireDatePicker.SelectedDate = reader["HireDate"] as DateTime?;
                                AddressTextBox.Text = reader["Address"].ToString();

                                // Pre-select Department
                                int deptId = reader["DepartmentID"] != DBNull.Value ? Convert.ToInt32(reader["DepartmentID"]) : -1;
                                foreach (ComboBoxItem item in DepartmentComboBox.Items)
                                {
                                    if ((int)item.Tag == deptId)
                                    {
                                        DepartmentComboBox.SelectedItem = item;
                                        break;
                                    }
                                }

                                // Pre-select Position
                                int posId = reader["PositionID"] != DBNull.Value ? Convert.ToInt32(reader["PositionID"]) : -1;
                                foreach (ComboBoxItem item in PositionComboBox.Items)
                                {
                                    if ((int)item.Tag == posId)
                                    {
                                        PositionComboBox.SelectedItem = item;
                                        break;
                                    }
                                }

                                BaseSalaryTextBox.Text = reader["BaseSalary"] != DBNull.Value
                                    ? ((decimal)reader["BaseSalary"]).ToString("F2")
                                    : "0.00";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading employee: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Load departments into ComboBox
        private void LoadDepartments()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    string query = "SELECT DepartmentID, DepartmentName FROM department";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        DepartmentComboBox.Items.Clear();
                        while (reader.Read())
                        {
                            DepartmentComboBox.Items.Add(new ComboBoxItem
                            {
                                Content = reader["DepartmentName"].ToString(),
                                Tag = Convert.ToInt32(reader["DepartmentID"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading departments: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Load positions into ComboBox
        private void LoadPositions()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    string query = "SELECT PositionID, PositionName FROM empositions";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        PositionComboBox.Items.Clear();
                        while (reader.Read())
                        {
                            PositionComboBox.Items.Add(new ComboBoxItem
                            {
                                Content = reader["PositionName"].ToString(),
                                Tag = Convert.ToInt32(reader["PositionID"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading positions: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // ----- OPTIONAL SECOND ROLE CHECK BEFORE SAVE -----
            string[] allowedRoles = { "HR", "Human Resources" };
            bool isAuthorized = false;
            if (!string.IsNullOrEmpty(UserSession.Role))
            {
                foreach (var role in allowedRoles)
                {
                    if (UserSession.Role.Contains(role, StringComparison.OrdinalIgnoreCase))
                    {
                        isAuthorized = true;
                        break;
                    }
                }
            }

            if (!isAuthorized)
            {
                MessageBox.Show("You are not authorized to save employee data.",
                                "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // ----------------------------------------------------

            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    string query = @"UPDATE employees
                             SET FirstName=@FirstName, MiddleName=@MiddleName, LastName=@LastName,
                                 Sex=@Sex, DOB=@DOB, ContactNumber=@ContactNumber, HireDate=@HireDate,
                                 Address=@Address, DepartmentID=@DepartmentID, PositionID=@PositionID,
                                 BaseSalary=@BaseSalary
                             WHERE EmployeeID=@EmployeeID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@FirstName", FirstNameTextBox.Text);
                        cmd.Parameters.AddWithValue("@MiddleName", string.IsNullOrEmpty(MiddleNameTextBox.Text) ? (object)DBNull.Value : MiddleNameTextBox.Text);
                        cmd.Parameters.AddWithValue("@LastName", LastNameTextBox.Text);
                        cmd.Parameters.AddWithValue("@Sex", ((ComboBoxItem)SexComboBox.SelectedItem)?.Content.ToString() ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DOB", DOBPicker.SelectedDate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ContactNumber", string.IsNullOrEmpty(ContactTextBox.Text) ? (object)DBNull.Value : ContactTextBox.Text);
                        cmd.Parameters.AddWithValue("@HireDate", HireDatePicker.SelectedDate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address", string.IsNullOrEmpty(AddressTextBox.Text) ? (object)DBNull.Value : AddressTextBox.Text);

                        int? deptId = (DepartmentComboBox.SelectedItem as ComboBoxItem)?.Tag as int?;
                        cmd.Parameters.AddWithValue("@DepartmentID", deptId ?? (object)DBNull.Value);

                        int? posId = (PositionComboBox.SelectedItem as ComboBoxItem)?.Tag as int?;
                        cmd.Parameters.AddWithValue("@PositionID", posId ?? (object)DBNull.Value);

                        decimal salary = decimal.TryParse(BaseSalaryTextBox.Text, out var s) ? s : 0;
                        cmd.Parameters.AddWithValue("@BaseSalary", salary);

                        cmd.Parameters.AddWithValue("@EmployeeID", _employeeId);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Employee updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // ✅ Trigger the parent to reload employees
                EmployeeSaved?.Invoke();

                // Close the window or UserControl host
                Window.GetWindow(this)?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating employee: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }
    }
}
