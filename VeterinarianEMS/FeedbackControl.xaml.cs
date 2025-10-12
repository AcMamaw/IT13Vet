using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace VeterinarianEMS
{
    public partial class FeedbackControl : UserControl
    {
        public class Feedback
        {
            public int FeedbackID { get; set; } // ✅ matches DB and DataGrid
            public string EmployeeName { get; set; }
            public string Category { get; set; } // Positive, Neutral, Negative
            public string Comment { get; set; }
            public DateTime Date { get; set; }
            public string Reviewed { get; set; }
        }

        private readonly string _connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True;";

        private List<Feedback> _allFeedbacks = new();
        private List<Feedback> _filteredFeedbacks = new();

        private int _currentPage = 1;
        private int _pageSize = 5;
        private int _totalPages = 1;

        private string _currentCategory = "Positive";

        public FeedbackControl()
        {
            InitializeComponent();

            if (EntriesSelector != null)
                EntriesSelector.EntriesChanged += EntriesSelector_EntriesChanged;

            LoadFeedbackFromDatabase();
            UpdateCategory(_currentCategory);
        }

        // 🔄 Load feedback from database
        private void LoadFeedbackFromDatabase()
        {
            _allFeedbacks.Clear();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"
                SELECT 
                    f.FeedbackID,
                    e.FirstName,
                    e.MiddleName,
                    e.LastName,
                    f.FeedbackType,
                    f.FeedbackText,
                    f.FeedbackDate,
                    f.Reviewed
                FROM Feedback f
                INNER JOIN Employees e ON f.EmployeeID = e.EmployeeID
                ORDER BY f.FeedbackDate DESC"; // initial DB order

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string firstName = reader.GetString(1);
                            string middleName = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            string lastName = reader.GetString(3);

                            string employeeFullName = firstName;
                            if (!string.IsNullOrWhiteSpace(middleName))
                                employeeFullName += " " + middleName[0] + ".";
                            employeeFullName += " " + lastName;

                            string reviewed = reader.IsDBNull(7) ? "No" : reader.GetString(7);

                            _allFeedbacks.Add(new Feedback
                            {
                                FeedbackID = reader.GetInt32(0),
                                EmployeeName = employeeFullName,
                                Category = reader.GetString(4),
                                Comment = reader.GetString(5),
                                Date = reader.GetDateTime(6),
                                Reviewed = reviewed
                            });
                        }
                    }
                }

                // 📝 Sort: unreviewed first, reviewed last, each group by Date descending
                _allFeedbacks = _allFeedbacks
                    .OrderBy(f => f.Reviewed?.Trim().ToLower() == "yes")  // unreviewed first
                    .ThenByDescending(f => f.Date)                        // newest first within group
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading feedback: " + ex.Message,
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔀 Change feedback category
        private void UpdateCategory(string category)
        {
            _currentCategory = category;
            _currentPage = 1;
            FilterFeedback();
        }

        // 🔍 Apply filter and search
        private void FilterFeedback()
        {
            string keyword = SearchBox.Text?.ToLower() ?? "";

            _filteredFeedbacks = _allFeedbacks
                .Where(f => f.Category == _currentCategory &&
                            (string.IsNullOrEmpty(keyword) ||
                             f.EmployeeName.ToLower().Contains(keyword) ||
                             f.Comment.ToLower().Contains(keyword)))
                .ToList();

            UpdateCounts();
            LoadPage();
        }

        // 🔢 Update counts on the buttons
        private void UpdateCounts()
        {
            PositiveCount.Text = _allFeedbacks.Count(f => f.Category == "Positive").ToString();
            NeutralCount.Text = _allFeedbacks.Count(f => f.Category == "Neutral").ToString();
            NegativeCount.Text = _allFeedbacks.Count(f => f.Category == "Negative").ToString();
        }

        // 📄 Load current page
        private void LoadPage()
        {
            if (_filteredFeedbacks.Count == 0)
            {
                FeedbackDataGrid.ItemsSource = null;
                UpdatePageInfo();
                return;
            }

            _totalPages = (int)Math.Ceiling((double)_filteredFeedbacks.Count / _pageSize);
            _currentPage = Math.Min(Math.Max(_currentPage, 1), _totalPages);

            var paged = _filteredFeedbacks
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

            FeedbackDataGrid.ItemsSource = paged;
            UpdatePageInfo();
        }

        // 🧾 Footer showing pagination info
        private void UpdatePageInfo()
        {
            int total = _filteredFeedbacks.Count;
            int start = total == 0 ? 0 : ((_currentPage - 1) * _pageSize) + 1;
            int end = Math.Min(start + _pageSize - 1, total);
            ShowingText.Text = $"Showing {_currentPage} to {_totalPages} of {total} entries";
            PageInfoTextBlock.Text = $"{_currentPage}";
        }

        // 🔘 Category Buttons
        private void PositiveButton_Click(object sender, RoutedEventArgs e) => UpdateCategory("Positive");
        private void NeutralButton_Click(object sender, RoutedEventArgs e) => UpdateCategory("Neutral");
        private void NegativeButton_Click(object sender, RoutedEventArgs e) => UpdateCategory("Negative");

        // ⏮ Pagination
        private void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadPage();
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadPage();
            }
        }

        // 🔍 Search
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentPage = 1;
            FilterFeedback();
        }

        // 🔢 Entries per page
        private void EntriesSelector_EntriesChanged(object sender, int entries)
        {
            _pageSize = entries;
            _currentPage = 1;
            LoadPage();
        }

        // ✅ Mark feedback as reviewed
        private void MarkReviewed_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Feedback fb)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(_connectionString))
                    {
                        conn.Open();

                        string query = "UPDATE Feedback SET Reviewed = 'Reviewed' WHERE FeedbackID = @FeedbackID";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@FeedbackID", fb.FeedbackID);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    fb.Reviewed = "Reviewed";

                    MessageBox.Show(
                        $"Feedback ID {fb.FeedbackID} marked as reviewed.",
                        "Feedback Reviewed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    LoadPage();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error marking feedback as reviewed: " + ex.Message);
                }
            }
        }

        // ❌ Delete feedback
        private void DeleteFeedback_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Feedback selected)
            {
                var confirm = MessageBox.Show($"Are you sure you want to delete feedback ID {selected.FeedbackID}?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(_connectionString))
                        {
                            conn.Open();
                            string deleteQuery = "DELETE FROM Feedback WHERE FeedbackID = @FeedbackID";
                            using (SqlCommand cmd = new SqlCommand(deleteQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@FeedbackID", selected.FeedbackID);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        _allFeedbacks.Remove(selected);
                        FilterFeedback();

                        MessageBox.Show("Feedback deleted successfully.",
                            "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error deleting feedback: " + ex.Message,
                            "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
