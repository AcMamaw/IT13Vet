using System;
using System.Windows;
using System.Windows.Controls;

namespace VeterinarianEMS.Controls
{
    public partial class EntriesPerPageSelector : UserControl
    {
        public event EventHandler<int> EntriesChanged;

        public EntriesPerPageSelector()
        {
            InitializeComponent();
        }

        public int SelectedEntries
        {
            get
            {
                if (EntriesComboBox.SelectedItem is ComboBoxItem item &&
                    int.TryParse(item.Content.ToString(), out int value))
                {
                    return value;
                }
                return 10; // default fallback
            }
        }

        private void EntriesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EntriesChanged?.Invoke(this, SelectedEntries);
        }
    }
}
