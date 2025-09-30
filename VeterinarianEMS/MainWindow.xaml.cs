using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Controls;

namespace VeterinarianEMS
{
    public partial class MainWindow : MetroWindow
    {

      
        public MainWindow()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            MainContentArea.Content = new DashboardControl();
        }


        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("You have been logged out.");
        }

        // Sidebar button click handlers
        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new DashboardControl();
        }

        private void AttendanceButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new AttendanceControl();
        }

        private void EmployeeButton_Click(object sender, RoutedEventArgs e)
        {
            // Replace with your EmployeeControl
            MainContentArea.Content = new EmployeeControl();
        }

        private void LeaveRequestButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new LeaveRequestControl();
        }

        private void OvertimeRequestButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new OvertimeRequestControl();
        }

        private void RolesButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new RolesControl();
        }

        private void ShiftsButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new ShiftsControl();
        }

        private void PayrollButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new PayrollControl();
        }
    }
}
