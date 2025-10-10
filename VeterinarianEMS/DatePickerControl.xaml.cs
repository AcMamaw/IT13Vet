using System;
using System.Windows;
using System.Windows.Controls;

namespace VeterinarianEMS.Controls
{
    public partial class DatePickerControl : UserControl
    {
        public DatePickerControl()
        {
            InitializeComponent();

            // Default: set today’s date
            SingleDatePicker.SelectedDate = DateTime.Now;
        }

        // 🔹 Expose public property
        public DateTime? SelectedDate
        {
            get => SingleDatePicker.SelectedDate;
            set => SingleDatePicker.SelectedDate = value;
        }
    }
}
