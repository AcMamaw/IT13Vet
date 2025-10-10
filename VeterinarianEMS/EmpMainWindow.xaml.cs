using System.Windows;
using VeterinarianEMS.Controls;
using static VeterinarianEMS.MainWindow;
using VeterinarianEMS.Controls;


namespace VeterinarianEMS.Views
{
    public partial class EmpMainWindow : MahApps.Metro.Controls.MetroWindow
    {

public EmpMainWindow()
    {
        this.WindowState = WindowState.Maximized;
        InitializeComponent();

        // Load Dashboard first
        MainContentArea.Content = new DashboardControl();

        // Bind user info for popup
        var userInfo = new
        {
            FullName = string.IsNullOrEmpty(UserSession.FullName) ? UserSession.Username : UserSession.FullName,
            Role = UserSession.Role
        };
        this.DataContext = userInfo;
    }


    // Dashboard button
    private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new DashboardControl();
        }

        private void OvertimeRequestButton_Click(object sender, RoutedEventArgs e)
        {
            // Load EmpOvertimeRequestControl in MainContentArea
            MainContentArea.Content = new EmpOvertimeRequestControl();
        }

        // Feedback button
        private void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new EmployeeFeedbackControl();
        }


        // Attendance button
        private void AttendanceButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new EmpAttendanceControl();
        }

        // Leave Requests button
        private void LeaveRequestButton_Click(object sender, RoutedEventArgs e)
        {
            // Load the Employee Leave Request control into the main content area
            MainContentArea.Content = new EmpLeaveRequestControl();
        }


        // Profile button
        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new EmployeeProfileControl();
        }


        // Logout button
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Show confirmation
            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to log out?",
                "Confirm Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // ✅ Clear session
                UserSession.Username = null;
                UserSession.FullName = null;
                UserSession.Role = null;

                // ✅ Back to login
                var loginWindow = new LoginRegisterWindow();
                loginWindow.Show();

                this.Close();
            }
            // If No is clicked, do nothing
        }

    }
}
