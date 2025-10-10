using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static VeterinarianEMS.MainWindow;

namespace VeterinarianEMS
{
    public partial class EmployeeFeedbackControl : UserControl
    {
        // 🧾 Feedback model
        public class Feedback
        {
            public int FeedbackID { get; set; }
            public int EmployeeID { get; set; }
            public string EmployeeName { get; set; }
            public DateTime Date { get; set; }
            public string Comment { get; set; }
            public string FeedbackType { get; set; } // Positive, Neutral, Negative
            public string Reviewed { get; set; } = "";
        }

        private string connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True;";

        private ObservableCollection<Feedback> _allFeedbacks = new();
        private ObservableCollection<Feedback> _filteredFeedbacks = new();

        // Pagination
        private int currentPage = 1;
        private int pageSize = 5;
        private int totalPages = 1;

        public EmployeeFeedbackControl()
        {
            InitializeComponent();
            LoadFeedbacks();
        }

        // 🔄 Load feedback for the logged-in employee
        private void LoadFeedbacks()
        {
            try
            {
                var list = new ObservableCollection<Feedback>();
                int? employeeId = UserSession.EmployeeID;

                if (employeeId == null)
                {
                    MessageBox.Show("No employee is currently logged in.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            f.FeedbackID,
                            f.EmployeeID,
                            e.FirstName,
                            e.MiddleName,
                            e.LastName,
                            f.FeedbackDate,
                            f.FeedbackText,
                            f.FeedbackType,
                            f.Reviewed
                        FROM Feedback f
                        LEFT JOIN Employees e ON f.EmployeeID = e.EmployeeID
                        WHERE f.EmployeeID = @EmployeeID
                        ORDER BY f.FeedbackDate DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeID", employeeId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string firstName = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                string middleName = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                string lastName = reader.IsDBNull(4) ? "" : reader.GetString(4);

                                string employeeName = firstName;
                                if (!string.IsNullOrWhiteSpace(middleName))
                                    employeeName += " " + middleName[0] + ".";
                                if (!string.IsNullOrWhiteSpace(lastName))
                                    employeeName += " " + lastName;

                                list.Add(new Feedback
                                {
                                    FeedbackID = reader.GetInt32(0),
                                    EmployeeID = reader.GetInt32(1),
                                    EmployeeName = employeeName,
                                    Date = reader.GetDateTime(5),
                                    Comment = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                    FeedbackType = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                    Reviewed = reader.IsDBNull(8) ? "" : reader.GetString(8)
                                });
                            }
                        }
                    }
                }

                _allFeedbacks = list;
                ApplySearchFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading feedbacks: " + ex.Message,
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔍 Apply search filter
        private void ApplySearchFilter()
        {
            string keyword = SearchBox.Text?.ToLower() ?? "";

            _filteredFeedbacks = string.IsNullOrWhiteSpace(keyword)
                ? new ObservableCollection<Feedback>(_allFeedbacks)
                : new ObservableCollection<Feedback>(_allFeedbacks.Where(f =>
                    f.EmployeeName.ToLower().Contains(keyword) ||
                    f.Comment.ToLower().Contains(keyword) ||
                    f.FeedbackType.ToLower().Contains(keyword)));

            currentPage = 1;
            LoadFeedbackPage();
        }

        // 📄 Load current page
        private void LoadFeedbackPage()
        {
            if (_filteredFeedbacks.Count == 0)
            {
                FeedbackDataGrid.ItemsSource = null;
                UpdatePageInfo();
                return;
            }

            totalPages = (int)Math.Ceiling((double)_filteredFeedbacks.Count / pageSize);
            currentPage = Math.Min(Math.Max(currentPage, 1), totalPages);

            var pageData = _filteredFeedbacks
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            FeedbackDataGrid.ItemsSource = pageData;
            UpdatePageInfo();
        }

        // 🔢 Update footer info
        private void UpdatePageInfo()
        {
            int total = _filteredFeedbacks.Count;
            int start = total == 0 ? 0 : ((currentPage - 1) * pageSize) + 1;
            int end = Math.Min(start + pageSize - 1, total);
            ShowingText.Text = $"Showing {start} to {end} of {total} entries";
        }

        // 🔍 Search changed
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearchFilter();
        }

        // ⏮ Previous
        private void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                LoadFeedbackPage();
            }
        }

        // ⏭ Next
        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage < totalPages)
            {
                currentPage++;
                LoadFeedbackPage();
            }
        }

        // 📝 Submit new feedback
        private void SubmitFeedback_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(FeedbackTextBox.Text))
            {
                MessageBox.Show("Please enter your feedback.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string type = PositiveRadio.IsChecked == true ? "Positive" :
                          NeutralRadio.IsChecked == true ? "Neutral" :
                          NegativeRadio.IsChecked == true ? "Negative" : null;

            if (type == null)
            {
                MessageBox.Show("Please select a feedback type.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // ✅ Get logged-in employee
                if (UserSession.EmployeeID == null)
                {
                    MessageBox.Show("No employee is currently logged in.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int employeeId = (int)UserSession.EmployeeID;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        INSERT INTO Feedback (EmployeeID, FeedbackDate, FeedbackText, FeedbackType)
                        VALUES (@EmployeeID, GETDATE(), @FeedbackText, @FeedbackType)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                        cmd.Parameters.AddWithValue("@FeedbackText", FeedbackTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@FeedbackType", type);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Clear inputs
                FeedbackTextBox.Clear();
                PositiveRadio.IsChecked = NeutralRadio.IsChecked = NegativeRadio.IsChecked = false;

                // Refresh DataGrid
                LoadFeedbacks();

                MessageBox.Show("Feedback submitted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error submitting feedback: " + ex.Message,
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
