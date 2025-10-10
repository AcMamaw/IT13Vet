using Microsoft.Data.SqlClient;
using System;
using System.Linq; // ✅ Needed for char.IsDigit check
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace VeterinarianEMS.Controls
{
    public partial class ShiftPopup : UserControl
    {
        public event Action<bool> OnClose;

        private string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True;";

        public int? EditingId { get; set; } = null; // null = insert, value = update

        public ShiftPopup()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (Resources["PopupShowAnimation"] is Storyboard sb)
                sb.Begin();
        }

        // ✅ Fix: Allow only numbers in Hour/Minute boxes
        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string shiftName = ShiftNameTextBox.Text.Trim();

            // ✅ Get Start Time
            string startHourText = StartHourTextBox.Text.Trim();
            string startMinuteText = StartMinuteTextBox.Text.Trim();
            string startAmPm = (StartAmPmComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            // ✅ Get End Time
            string endHourText = EndHourTextBox.Text.Trim();
            string endMinuteText = EndMinuteTextBox.Text.Trim();
            string endAmPm = (EndAmPmComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            // 🔹 Validation
            if (string.IsNullOrEmpty(shiftName) ||
                string.IsNullOrEmpty(startHourText) || string.IsNullOrEmpty(startMinuteText) || string.IsNullOrEmpty(startAmPm) ||
                string.IsNullOrEmpty(endHourText) || string.IsNullOrEmpty(endMinuteText) || string.IsNullOrEmpty(endAmPm))
            {
                MessageBox.Show("Please fill in all fields.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 🔹 Convert Start Time
            if (!int.TryParse(startHourText, out int startHour) ||
                !int.TryParse(startMinuteText, out int startMinute) ||
                startHour < 1 || startHour > 12 || startMinute < 0 || startMinute > 59)
            {
                MessageBox.Show("Invalid start time.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (startAmPm == "PM" && startHour != 12) startHour += 12;
            if (startAmPm == "AM" && startHour == 12) startHour = 0;
            TimeSpan startTime = new TimeSpan(startHour, startMinute, 0);

            // 🔹 Convert End Time
            if (!int.TryParse(endHourText, out int endHour) ||
                !int.TryParse(endMinuteText, out int endMinute) ||
                endHour < 1 || endHour > 12 || endMinute < 0 || endMinute > 59)
            {
                MessageBox.Show("Invalid end time.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (endAmPm == "PM" && endHour != 12) endHour += 12;
            if (endAmPm == "AM" && endHour == 12) endHour = 0;
            TimeSpan endTime = new TimeSpan(endHour, endMinute, 0);

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query;

                    if (EditingId.HasValue)
                    {
                        // ✅ Update
                        query = @"UPDATE shifts 
                                  SET ShiftName = @ShiftName, StartTime = @StartTime, EndTime = @EndTime 
                                  WHERE ShiftID = @Id";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@ShiftName", shiftName);
                            cmd.Parameters.AddWithValue("@StartTime", startTime);
                            cmd.Parameters.AddWithValue("@EndTime", endTime);
                            cmd.Parameters.AddWithValue("@Id", EditingId.Value);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Shift updated successfully!",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // ✅ Insert
                        query = @"INSERT INTO shifts (ShiftName, StartTime, EndTime) 
                                  VALUES (@ShiftName, @StartTime, @EndTime)";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@ShiftName", shiftName);
                            cmd.Parameters.AddWithValue("@StartTime", startTime);
                            cmd.Parameters.AddWithValue("@EndTime", endTime);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Shift saved successfully!",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                // Notify parent
                OnClose?.Invoke(true);
                CloseParentWindow();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving shift: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            OnClose?.Invoke(false);
            CloseParentWindow();
        }

        private void CloseParentWindow()
        {
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
                parentWindow.Close();
        }
    }
}
