using System;
using System.Windows;
using System.Windows.Controls;
using VeterinarianEMS.Controls; // so you can access DateRangePicker

namespace VeterinarianEMS
{
    public partial class PayrollControl : UserControl
    {
        public PayrollControl()
        {
            InitializeComponent();
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            var start = MyDateRange.StartDate;
            var end = MyDateRange.EndDate;

            MessageBox.Show($"Start: {start?.ToShortDateString()}, End: {end?.ToShortDateString()}");
        }

        private void EmployeeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Example: get selected item
            ComboBox comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                ComboBoxItem selectedItem = comboBox.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    string employeeName = selectedItem.Content.ToString();
                    // Do something with employeeName
                }
            }
        }


    }
}
