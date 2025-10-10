using System;
using System.Windows;
using System.Windows.Controls;

namespace VeterinarianEMS.Controls
{
    public partial class DateRangePicker : UserControl
    {
        public DateRangePicker()
        {
            InitializeComponent();

            // Detect current month from system
            var now = DateTime.Now;

            // First day of current month
            var firstDay = new DateTime(now.Year, now.Month, 1);

            // Last day of current month
            var lastDay = firstDay.AddMonths(1).AddDays(-1);

            // Set default values
            StartDatePicker.SelectedDate = firstDay;
            EndDatePicker.SelectedDate = lastDay;
        }

        // 🔹 Public properties you can call outside
        public DateTime? StartDate
        {
            get => StartDatePicker.SelectedDate;
            set => StartDatePicker.SelectedDate = value;
        }

        public DateTime? EndDate
        {
            get => EndDatePicker.SelectedDate;
            set => EndDatePicker.SelectedDate = value;
        }
    }
}
