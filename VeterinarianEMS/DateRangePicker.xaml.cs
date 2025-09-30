using System;
using System.Windows.Controls;

namespace VeterinarianEMS.Controls
{
    public partial class DateRangePicker : UserControl
    {
        public DateRangePicker()
        {
            InitializeComponent();
        }

        // Property for Start Date
        private DateTime? _startDate;
        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                StartDatePicker.SelectedDate = value; // Show in picker
            }
        }

        // Property for End Date
        private DateTime? _endDate;
        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                EndDatePicker.SelectedDate = value; // Show in picker
            }
        }
    }
}
