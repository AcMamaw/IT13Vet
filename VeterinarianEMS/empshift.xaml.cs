using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace VeterinarianEMS.Controls
{
    public partial class EmpShift : UserControl
    {
        // Connection string (update if needed)
        private readonly string _connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True";

        // Employee ID to assign shift for
        public int EmployeeID { get; set; }

        // Event to notify parent window that a shift was saved
        public event Action ShiftSaved;

        public EmpShift()
        {
            InitializeComponent();
            LoadShifts(); // Load shifts into ComboBox when control is initialized
        }

        #region Load Shifts

        private void LoadShifts()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "SELECT ShiftID, ShiftName FROM Shifts ORDER BY ShiftName";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        var shifts = new List<ShiftItem>();
                        while (reader.Read())
                        {
                            shifts.Add(new ShiftItem
                            {
                                ShiftID = reader.GetInt32(0),
                                ShiftName = reader.GetString(1)
                            });
                        }

                        ShiftComboBox.ItemsSource = shifts;
                        ShiftComboBox.DisplayMemberPath = "ShiftName";
                        ShiftComboBox.SelectedValuePath = "ShiftID";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading shifts:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Helper class for ComboBox
        private class ShiftItem
        {
            public int ShiftID { get; set; }
            public string ShiftName { get; set; }
        }

        #endregion

        #region All Days Logic

        private void AllDaysCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SetAllDays(true);
        }

        private void AllDaysCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SetAllDays(false);
        }

        private void DayCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (AreAllDaysChecked())
            {
                AllDaysCheckBox.Checked -= AllDaysCheckBox_Checked;
                AllDaysCheckBox.IsChecked = true;
                AllDaysCheckBox.Checked += AllDaysCheckBox_Checked;
            }
        }

        private void DayCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (AllDaysCheckBox.IsChecked == true)
            {
                AllDaysCheckBox.Unchecked -= AllDaysCheckBox_Unchecked;
                AllDaysCheckBox.IsChecked = false;
                AllDaysCheckBox.Unchecked += AllDaysCheckBox_Unchecked;
            }
        }

        private void SetAllDays(bool value)
        {
            MondayCheckBox.IsChecked = value;
            TuesdayCheckBox.IsChecked = value;
            WednesdayCheckBox.IsChecked = value;
            ThursdayCheckBox.IsChecked = value;
            FridayCheckBox.IsChecked = value;
            SaturdayCheckBox.IsChecked = value;
        }

        private bool AreAllDaysChecked()
        {
            return MondayCheckBox.IsChecked == true &&
                   TuesdayCheckBox.IsChecked == true &&
                   WednesdayCheckBox.IsChecked == true &&
                   ThursdayCheckBox.IsChecked == true &&
                   FridayCheckBox.IsChecked == true &&
                   SaturdayCheckBox.IsChecked == true;
        }

        #endregion

        #region Save / Cancel Logic
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (ShiftComboBox.SelectedValue == null)
            {
                MessageBox.Show("Please select a shift.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int shiftId = (int)ShiftComboBox.SelectedValue;
            List<string> selectedDays = GetSelectedDays();

            if (selectedDays.Count == 0)
            {
                MessageBox.Show("Please select at least one day.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string daysString = string.Join(", ", selectedDays);

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Check if an entry already exists for this employee & shift
                    string checkQuery = "SELECT COUNT(*) FROM EmployeeShifts WHERE EmployeeID = @EmployeeID AND ShiftID = @ShiftID";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@EmployeeID", EmployeeID);
                        checkCmd.Parameters.AddWithValue("@ShiftID", shiftId);

                        int count = (int)checkCmd.ExecuteScalar();

                        if (count > 0)
                        {
                            // Update existing record
                            string updateQuery = "UPDATE EmployeeShifts SET ShiftDays = @ShiftDays WHERE EmployeeID = @EmployeeID AND ShiftID = @ShiftID";
                            using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                            {
                                updateCmd.Parameters.AddWithValue("@ShiftDays", daysString);
                                updateCmd.Parameters.AddWithValue("@EmployeeID", EmployeeID);
                                updateCmd.Parameters.AddWithValue("@ShiftID", shiftId);
                                updateCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // Insert new record
                            string insertQuery = "INSERT INTO EmployeeShifts (EmployeeID, ShiftID, ShiftDays) VALUES (@EmployeeID, @ShiftID, @ShiftDays)";
                            using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                            {
                                insertCmd.Parameters.AddWithValue("@EmployeeID", EmployeeID);
                                insertCmd.Parameters.AddWithValue("@ShiftID", shiftId);
                                insertCmd.Parameters.AddWithValue("@ShiftDays", daysString);
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                MessageBox.Show("Shift assigned successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Fire event to notify parent
                ShiftSaved?.Invoke();

                // Optional: clear selections
                ShiftComboBox.SelectedIndex = -1;
                SetAllDays(false);

                // 🔹 Close the parent window
                Window.GetWindow(this)?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving to database:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Close the parent window
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.Close();
            }
        }


        private List<string> GetSelectedDays()
        {
            List<string> days = new List<string>();
            if (MondayCheckBox.IsChecked == true) days.Add("Monday");
            if (TuesdayCheckBox.IsChecked == true) days.Add("Tuesday");
            if (WednesdayCheckBox.IsChecked == true) days.Add("Wednesday");
            if (ThursdayCheckBox.IsChecked == true) days.Add("Thursday");
            if (FridayCheckBox.IsChecked == true) days.Add("Friday");
            if (SaturdayCheckBox.IsChecked == true) days.Add("Saturday");
            return days;
        }

        #endregion
    }
}
