using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Controls;
using VeterinarianEMS.Controls;
using VeterinarianEMS.Views;

namespace VeterinarianEMS
{
    public partial class MainWindow : MetroWindow
    {
        public static class UserSession
        {
            public static int? UserId { get; set; }
            public static int? EmployeeID { get; set; }  // ✅ Add this
            public static string Username { get; set; }
            public static string FullName { get; set; }
            public static string Role { get; set; }

        }


        public MainWindow()
        {
            InitializeComponent();

            this.WindowState = WindowState.Maximized;

            // ✅ Load Dashboard first
            MainContentArea.Content = new DashboardControl();

            // ✅ Bind user info for popup
            var userInfo = new
            {
                FullName = string.IsNullOrEmpty(UserSession.FullName) ? UserSession.Username : UserSession.FullName,
                Role = UserSession.Role
            };

            this.DataContext = userInfo;
        }


        public void RefreshData()
        {
            Console.WriteLine("MainWindow refreshed!");
}


        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Show confirmation dialog
            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to log out?",
                "Confirm Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Clear session
                UserSession.Username = null;
                UserSession.FullName = null;
                UserSession.Role = null;

                // Open Login/Register window
                LoginRegisterWindow loginWindow = new LoginRegisterWindow();
                loginWindow.Show();

                // Close current window
                this.Close();
            }
            // If user clicks No, do nothing
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

        private void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainContentArea.Content = new FeedbackControl();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while loading FeedbackControl:\n{ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
