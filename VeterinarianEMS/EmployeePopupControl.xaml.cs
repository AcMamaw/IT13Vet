using System;
using System.Windows;
using System.Windows.Controls;
using BCrypt.Net;
using Microsoft.Data.SqlClient;

namespace VeterinarianEMS.Views
{
    public partial class EmployeePopupControl : UserControl
    {
        public event Action EmployeeSaved;
        private readonly string connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True;";

        public EmployeePopupControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDepartments();
            LoadPositions();
        }

        private void LoadDepartments()
        {
            try
            {
                DepartmentComboBox.Items.Clear();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT DepartmentID, DepartmentName FROM Department", conn);
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        // Create an item with both ID and Name
                        ComboBoxItem item = new ComboBoxItem
                        {
                            Content = reader["DepartmentName"].ToString(),
                            Tag = reader["DepartmentID"] // store DepartmentID
                        };

                        DepartmentComboBox.Items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading departments: " + ex.Message);
            }
        }

        private void LoadPositions()
        {
            try
            {
                PositionComboBox.Items.Clear();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT positionID, positionName FROM empositions", conn);
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        ComboBoxItem item = new ComboBoxItem
                        {
                            Content = reader["positionName"].ToString(),
                            Tag = reader["positionID"] // store ID in Tag
                        };

                        PositionComboBox.Items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading positions: " + ex.Message);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get values from controls
                string firstName = FirstNameTextBox.Text.Trim();
                string middleName = MiddleNameTextBox.Text.Trim();
                string lastName = LastNameTextBox.Text.Trim();
                string sex = (SexComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                string email = EmailTextBox.Text.Trim();
                int? positionId = null;
                int? departmentId = null;

                if (PositionComboBox.SelectedItem is ComboBoxItem posItem && posItem.Tag != null)
                    positionId = Convert.ToInt32(posItem.Tag);

                if (DepartmentComboBox.SelectedItem is ComboBoxItem deptItem && deptItem.Tag != null)
                    departmentId = Convert.ToInt32(deptItem.Tag);

                string contact = ContactTextBox.Text.Trim();
                string baseSalaryStr = BaseSalaryTextBox.Text.Trim();
                DateTime? hireDate = HireDatePicker.SelectedDate;
                string address = AddressTextBox.Text.Trim();
                DateTime? dob = DOBPicker.SelectedDate;

                string username = UsernameTextBox.Text.Trim();
                string password = PasswordTextBox.Text.Trim();

                // HourlyRate input
                string hourlyRateStr = HourlyRateTextBox.Text.Trim();
                decimal? hourlyRate = null;
                if (!string.IsNullOrEmpty(hourlyRateStr) && decimal.TryParse(hourlyRateStr, out decimal hr))
                    hourlyRate = hr;

                // Optional: auto compute base salary from hourly rate if base salary empty
                decimal? baseSalary = null;
                if (!string.IsNullOrEmpty(baseSalaryStr) && decimal.TryParse(baseSalaryStr, out decimal bs))
                    baseSalary = bs;
                else if (hourlyRate.HasValue)
                    baseSalary = hourlyRate.Value * 160; // example 160 working hours/month

                // Validate fields
                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    MessageBox.Show("First Name and Last Name are required.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Username and Password are required.");
                    return;
                }

                // Calculate Age from DOB
                int? age = null;
                if (dob.HasValue)
                {
                    DateTime today = DateTime.Today;
                    age = today.Year - dob.Value.Year;
                    if (dob.Value.Date > today.AddYears(-age.Value)) age--;
                }

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        // Step 1: Insert into Employees
                        string insertEmployeeQuery = @"
                INSERT INTO Employees
                (FirstName, MiddleName, LastName, Sex, Age, DOB, PositionID, DepartmentID,
                 ContactNumber, BaseSalary, HourlyRate, HireDate, Address, Email)
                OUTPUT INSERTED.EmployeeID
                VALUES
                (@FirstName, @MiddleName, @LastName, @Sex, @Age, @DOB, @PositionID, @DepartmentID,
                 @ContactNumber, @BaseSalary, @HourlyRate, @HireDate, @Address, @Email)";

                        int newEmployeeId;

                        using (SqlCommand cmd = new SqlCommand(insertEmployeeQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@FirstName", firstName);
                            cmd.Parameters.AddWithValue("@MiddleName", string.IsNullOrEmpty(middleName) ? DBNull.Value : (object)middleName);
                            cmd.Parameters.AddWithValue("@LastName", lastName);
                            cmd.Parameters.AddWithValue("@Sex", (object)sex ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Age", age.HasValue ? age.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@DOB", dob.HasValue ? dob.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@PositionID", positionId.HasValue ? positionId.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@DepartmentID", departmentId.HasValue ? departmentId.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@ContactNumber", string.IsNullOrEmpty(contact) ? DBNull.Value : (object)contact);
                            cmd.Parameters.AddWithValue("@BaseSalary", baseSalary.HasValue ? baseSalary.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@HourlyRate", hourlyRate.HasValue ? hourlyRate.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@HireDate", hireDate.HasValue ? hireDate.Value : DateTime.Now);
                            cmd.Parameters.AddWithValue("@Address", string.IsNullOrEmpty(address) ? DBNull.Value : (object)address);
                            cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(email) ? DBNull.Value : (object)email);

                            newEmployeeId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // Step 2: Hash password
                        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

                        // Step 3: Insert into Users
                        string insertUserQuery = @"
                INSERT INTO Users (Username, Password, PositionID, EmployeeID)
                VALUES (@Username, @Password, @PositionID, @EmployeeID)";

                        using (SqlCommand cmdUser = new SqlCommand(insertUserQuery, conn, transaction))
                        {
                            cmdUser.Parameters.AddWithValue("@Username", username);
                            cmdUser.Parameters.AddWithValue("@Password", hashedPassword);
                            cmdUser.Parameters.AddWithValue("@PositionID", positionId.HasValue ? positionId.Value : DBNull.Value);
                            cmdUser.Parameters.AddWithValue("@EmployeeID", newEmployeeId);

                            cmdUser.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        MessageBox.Show("Employee and user account saved successfully!");
                        EmployeeSaved?.Invoke();
                        Window.GetWindow(this)?.Close();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show("Error saving data: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }



        private void GeneratePassword_Click(object sender, RoutedEventArgs e)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            string password = new string(Enumerable.Repeat(chars, 8) // now 8 characters
                .Select(s => s[random.Next(s.Length)]).ToArray());

            PasswordTextBox.Text = password;
        }


        private void HourlyRateTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Parse hourly rate
            if (decimal.TryParse(HourlyRateTextBox.Text.Trim(), out decimal hourlyRate))
            {
                // Example: assume 160 working hours per month
                decimal baseSalary = hourlyRate * 160;
                BaseSalaryTextBox.Text = baseSalary.ToString("0.00");
            }
            else
            {
                BaseSalaryTextBox.Text = "0.00";
            }
        }

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Automatically set username to match email
            UsernameTextBox.Text = EmailTextBox.Text.Trim();
        }


        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.Close();
            }
        }

        private void ClearForm()
        {
            FirstNameTextBox.Clear();
            MiddleNameTextBox.Clear();
            LastNameTextBox.Clear();
            SexComboBox.SelectedIndex = -1;
            PositionComboBox.SelectedIndex = -1;
            DepartmentComboBox.SelectedIndex = -1;
            DOBPicker.SelectedDate = null;
            ContactTextBox.Clear();
            BaseSalaryTextBox.Clear();
            HireDatePicker.SelectedDate = DateTime.Now;
            AddressTextBox.Clear();
        }
    }
}
