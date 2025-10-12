using Microsoft.Data.SqlClient;
using System;
using System.Windows;
using System.Windows.Controls;

namespace VeterinarianEMS.Views
{
    public partial class EmpEditProfileControl : UserControl
    {
        public event Action EmployeeProfileSaved;
        private readonly string _connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True;";

        private int _employeeID;

        public EmpEditProfileControl(int employeeID)
        {
            InitializeComponent();
            _employeeID = employeeID;
            LoadEmployeeData();
        }

        private void LoadEmployeeData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT FirstName, MiddleName, LastName, Email, ContactNumber, 
                               Sex, DOB, Address
                        FROM employees
                        WHERE EmployeeID = @EmployeeID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeID", _employeeID);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                FirstNameTextBox.Text = reader["FirstName"].ToString();
                                MiddleNameTextBox.Text = reader["MiddleName"].ToString();
                                LastNameTextBox.Text = reader["LastName"].ToString();
                                EmailTextBox.Text = reader["Email"].ToString();
                                ContactTextBox.Text = reader["ContactNumber"].ToString();
                                AddressTextBox.Text = reader["Address"].ToString();

                                // Sex
                                string sex = reader["Sex"].ToString();
                                if (!string.IsNullOrEmpty(sex))
                                {
                                    SexComboBox.SelectedIndex = sex == "Male" ? 0 : 1;
                                }

                                // Date of Birth
                                if (reader["DOB"] != DBNull.Value)
                                {
                                    DOBPicker.SelectedDate = Convert.ToDateTime(reader["DOB"]);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Employee not found.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading data: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(LastNameTextBox.Text))
                {
                    MessageBox.Show("First name and last name are required.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string selectedSex = (SexComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string updateQuery = @"
                UPDATE employees
                SET FirstName = @FirstName,
                    MiddleName = @MiddleName,
                    LastName = @LastName,
                    Email = @Email,
                    ContactNumber = @ContactNumber,
                    Sex = @Sex,
                    DOB = @DOB,
                    Address = @Address
                WHERE EmployeeID = @EmployeeID";

                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@FirstName", FirstNameTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@MiddleName", MiddleNameTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@LastName", LastNameTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@Email", EmailTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@ContactNumber", ContactTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@Sex", selectedSex ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DOB", DOBPicker.SelectedDate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address", AddressTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@EmployeeID", _employeeID);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Employee information updated successfully!",
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                            // Trigger event to refresh main view
                            EmployeeProfileSaved?.Invoke();

                            // Close the parent window
                            Window.GetWindow(this)?.Close();
                        }
                        else
                        {
                            MessageBox.Show("No records were updated.",
                                "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating employee: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Optional: Close popup or clear fields
            Window.GetWindow(this)?.Close();
        }
    }
}
