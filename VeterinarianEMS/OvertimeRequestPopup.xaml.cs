using Microsoft.Data.SqlClient;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static VeterinarianEMS.MainWindow;

namespace VeterinarianEMS.Controls
{
    public partial class OvertimeRequestPopup : UserControl
    {
        private readonly string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True;";

        // 🔹 Event to notify parent control after successful save
        public event Action OnSaved;

        public OvertimeRequestPopup()
        {
            InitializeComponent();
        }

        // 🔹 Allow only numbers in time fields
        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        // 🔹 Save Button Logic (with DB insert)
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DateTime? overtimeDate = OvertimeDatePicker.SelectedDate;
            string startHour = StartHourTextBox.Text.Trim();
            string startMinute = StartMinuteTextBox.Text.Trim();
            string startAmPm = (StartAmPmComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            string endHour = EndHourTextBox.Text.Trim();
            string endMinute = EndMinuteTextBox.Text.Trim();
            string endAmPm = (EndAmPmComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            // ✅ Validation
            if (overtimeDate == null ||
                string.IsNullOrEmpty(startHour) || string.IsNullOrEmpty(startMinute) ||
                string.IsNullOrEmpty(endHour) || string.IsNullOrEmpty(endMinute) ||
                startAmPm == null || endAmPm == null)
            {
                MessageBox.Show("Please complete all fields before saving.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int? employeeId = UserSession.EmployeeID;
            if (employeeId == null)
            {
                MessageBox.Show("Cannot determine your Employee ID. Please make sure you are logged in.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // ✅ Convert to 24-hour format
                int sh = int.Parse(startHour);
                int sm = int.Parse(startMinute);
                int eh = int.Parse(endHour);
                int em = int.Parse(endMinute);

                if (sh > 12 || sm > 59 || eh > 12 || em > 59)
                {
                    MessageBox.Show("Invalid time format entered.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (startAmPm == "PM" && sh != 12) sh += 12;
                if (startAmPm == "AM" && sh == 12) sh = 0;
                if (endAmPm == "PM" && eh != 12) eh += 12;
                if (endAmPm == "AM" && eh == 12) eh = 0;

                TimeSpan startTime = new TimeSpan(sh, sm, 0);
                TimeSpan endTime = new TimeSpan(eh, em, 0);

                if (endTime <= startTime)
                {
                    MessageBox.Show("End time must be after start time.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ✅ Insert into database
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string insertQuery = @"
                        INSERT INTO overtimerequests (EmployeeID, OvertimeDate, StartTime, EndTime, Status)
                        VALUES (@EmployeeID, @OvertimeDate, @StartTime, @EndTime, @Status)";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeID", employeeId.Value);
                        cmd.Parameters.AddWithValue("@OvertimeDate", overtimeDate.Value.Date);
                        cmd.Parameters.AddWithValue("@StartTime", startTime);
                        cmd.Parameters.AddWithValue("@EndTime", endTime);
                        cmd.Parameters.AddWithValue("@Status", "Pending");

                        cmd.ExecuteNonQuery();
                    }
                }

                // ✅ Success message
                MessageBox.Show("Overtime request successfully added!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // ✅ Trigger refresh event for parent control
                OnSaved?.Invoke();

                // ✅ Close the popup
                Window.GetWindow(this)?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving overtime request:\n{ex.Message}",
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔹 Cancel Button Logic
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }
    }
}
