using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using static VeterinarianEMS.MainWindow;

namespace VeterinarianEMS.Controls
{
    public partial class EmpShift : UserControl
    {
        // Property to store selected employee ID
        public int EmployeeID { get; set; }

        // Event to notify parent that shift has been saved
        public event Action ShiftSaved;

        // Connection string
        private readonly string _connString = @"Data Source=(localdb)\MSSQLLocalDB;
                                                Initial Catalog=VeterinarianEMS;
                                                Integrated Security=True;";


        public EmpShift()
        {
            InitializeComponent();

            // Check if current user has HR role or Human Resources
            if (UserSession.Role == null ||
                !(UserSession.Role.Contains("HR", StringComparison.OrdinalIgnoreCase) ||
                  UserSession.Role.Contains("Human Resources", StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("You are not authorized to manage employee shifts.",
                                "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Close the window hosting this control
                Window.GetWindow(this)?.Close();
                return;
            }

            // Only load shifts if authorized
            LoadShifts();
        }


        private void LoadShifts()
        {
            try
            {
                List<ShiftModel> shifts = new List<ShiftModel>();

                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    string query = "SELECT ShiftID, ShiftName FROM shifts";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            shifts.Add(new ShiftModel
                            {
                                ShiftID = Convert.ToInt32(reader["ShiftID"]),
                                ShiftName = reader["ShiftName"].ToString()
                            });
                        }
                    }
                }

                ShiftComboBox.ItemsSource = shifts;
                ShiftComboBox.DisplayMemberPath = "ShiftName";
                ShiftComboBox.SelectedValuePath = "ShiftID";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading shifts: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (ShiftComboBox.SelectedValue == null)
            {
                MessageBox.Show("Please select a shift.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int shiftId = (int)ShiftComboBox.SelectedValue;

            // Collect selected days
            List<string> selectedDays = new List<string>();
            if (MondayCheckBox.IsChecked == true) selectedDays.Add("Monday");
            if (TuesdayCheckBox.IsChecked == true) selectedDays.Add("Tuesday");
            if (WednesdayCheckBox.IsChecked == true) selectedDays.Add("Wednesday");
            if (ThursdayCheckBox.IsChecked == true) selectedDays.Add("Thursday");
            if (FridayCheckBox.IsChecked == true) selectedDays.Add("Friday");
            if (SaturdayCheckBox.IsChecked == true) selectedDays.Add("Saturday");

            if (selectedDays.Count == 0)
            {
                MessageBox.Show("Please select at least one day.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string daysText = string.Join(",", selectedDays); // Store as comma-separated string

            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();

                    string query = @"
                INSERT INTO employeeshifts (EmployeeID, ShiftID, ShiftDays)
                VALUES (@EmployeeID, @ShiftID, @ShiftDays)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeID", EmployeeID);
                        cmd.Parameters.AddWithValue("@ShiftID", shiftId);
                        cmd.Parameters.AddWithValue("@ShiftDays", daysText);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show($"EmployeeID: {EmployeeID}\nShiftID: {shiftId}\nDays: {daysText}\n\nShift saved successfully!",
                                "Shift Saved", MessageBoxButton.OK, MessageBoxImage.Information);

                // Trigger the parent reload
                ShiftSaved?.Invoke();

                // Close this shift window
                Window.GetWindow(this)?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving shift: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }

        /// <summary>
        /// Shift model
        /// </summary>
        public class ShiftModel
        {
            public int ShiftID { get; set; }
            public string ShiftName { get; set; }
        }
    }
}
