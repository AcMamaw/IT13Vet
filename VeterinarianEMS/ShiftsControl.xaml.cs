using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VeterinarianEMS.Controls;
using VeterinarianEMS.Models;
using static VeterinarianEMS.MainWindow;

namespace VeterinarianEMS
{
    public partial class ShiftsControl : UserControl
    {
        private readonly string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True;";

        // Master list
        private List<ShiftModel> _allShifts = new List<ShiftModel>();

        // Filtered list
        private List<ShiftModel> _filteredShifts = new List<ShiftModel>();

        // Pagination
        private int currentPage = 1;
        private int pageSize = 5;   // ✅ default to 5
        private int totalPages = 1;

        public ShiftsControl()
        {
            InitializeComponent();

            // hook EntriesSelector event
            EntriesSelector.EntriesChanged += EntriesSelector_OnEntriesChanged;

            Loaded += ShiftsControl_Loaded;
        }

        private void ShiftsControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadShifts();
        }

        // 🔄 Load shifts from DB
        private void LoadShifts()
        {
            try
            {
                var shifts = new List<ShiftModel>();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT ShiftID, ShiftName, StartTime, EndTime
                        FROM shifts
                        ORDER BY ShiftID ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        int i = 1;
                        while (reader.Read())
                        {
                            shifts.Add(new ShiftModel
                            {
                                Number = i++,
                                Id = reader.GetInt32(0),
                                Name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                StartTime = reader.IsDBNull(2) ? "" : reader.GetTimeSpan(2).ToString(@"hh\:mm"), // ✅ fixed
                                EndTime = reader.IsDBNull(3) ? "" : reader.GetTimeSpan(3).ToString(@"hh\:mm")  // ✅ fixed
                            });
                        }
                    }
                }

                _allShifts = shifts;
                ApplySearchFilter(); // refresh
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading shifts: " + ex.Message);
            }
        }

        // 🔎 Apply search filter
        private void ApplySearchFilter()
        {
            string keyword = SearchTextBox.Text?.ToLower() ?? "";

            _filteredShifts = string.IsNullOrWhiteSpace(keyword)
                ? _allShifts.ToList()
                : _allShifts.Where(x =>
                        x.Id.ToString().Contains(keyword) ||
                        x.Name.ToLower().Contains(keyword) ||
                        x.StartTime.ToLower().Contains(keyword) ||
                        x.EndTime.ToLower().Contains(keyword))
                    .ToList();

            currentPage = 1;
            LoadShiftsPage();
        }

        // 📄 Load current page
        private void LoadShiftsPage()
        {
            if (_filteredShifts.Count == 0)
            {
                ShiftsDataGrid.ItemsSource = null;
                UpdatePageInfo();
                return;
            }

            totalPages = (int)Math.Ceiling((double)_filteredShifts.Count / pageSize);
            currentPage = Math.Min(Math.Max(currentPage, 1), totalPages);

            var pagedShifts = _filteredShifts
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ShiftsDataGrid.ItemsSource = pagedShifts;
            UpdatePageInfo();
        }

        // 🔄 Update page info display
        private void UpdatePageInfo()
        {
            if (PageInfoTextBlock != null)
            {
                // ✅ Show current page and total pages
                PageInfoTextBlock.Text = $"{currentPage}";
            }

            if (EntriesInfoTextBlock != null)
            {
                int start = _filteredShifts.Count == 0 ? 0 : ((currentPage - 1) * pageSize) + 1;
                int end = _filteredShifts.Count == 0 ? 0 : Math.Min(start + pageSize - 1, _filteredShifts.Count);

                // ✅ Keep showing entry range
                EntriesInfoTextBlock.Text = $"Showing {currentPage} to {totalPages} of {_filteredShifts.Count} entries";
            }
        }


        // 🔄 Search box
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearchFilter();
        }

        // ⏮ Prev
        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                LoadShiftsPage();
            }
        }

        // ⏭ Next
        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage < totalPages)
            {
                currentPage++;
                LoadShiftsPage();
            }
        }

        // 🔄 Entries per page changed
        private void EntriesSelector_OnEntriesChanged(object sender, int newPageSize)
        {
            pageSize = newPageSize;
            currentPage = 1;
            LoadShiftsPage();
        }

        // ➕ Add Shift (with try-catch and HR authorization)
        private void AddShift_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 🔒 Authorization check
                if (UserSession.Role == null ||
                    !(UserSession.Role.Contains("HR", StringComparison.OrdinalIgnoreCase) ||
                      UserSession.Role.Contains("Human Resources", StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("You are not authorized to add shifts.",
                        "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var popup = new ShiftPopup();

                // when popup closes, reload if needed
                popup.OnClose += (reload) =>
                {
                    if (reload)
                        LoadShifts();
                };

                // ✅ fix: safely set owner only if it's not itself
                Window ownerWindow = Window.GetWindow(this);
                Window popupWindow = new Window
                {
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.NoResize,
                    AllowsTransparency = true,
                    Background = Brushes.Transparent,
                    Content = popup,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                // only set owner if not the same window
                if (ownerWindow != popupWindow)
                    popupWindow.Owner = ownerWindow;

                popupWindow.ShowDialog();
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show("Database error while adding shift:\n" + sqlEx.Message,
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (InvalidOperationException invEx)
            {
                MessageBox.Show("Operation error while opening the Add Shift popup:\n" + invEx.Message,
                    "Operation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected error while adding shift:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✏ Edit
        private void EditShift_Click(object sender, RoutedEventArgs e)
        {
            if (ShiftsDataGrid.SelectedItem is ShiftModel selectedShift)
            {
                MessageBox.Show($"Edit Shift: {selectedShift.Name} ({selectedShift.StartTime} - {selectedShift.EndTime})");
                // TODO: Open popup like AddShift but pre-filled with data
            }
        }

        // ❌ Delete
        private void DeleteShift_Click(object sender, RoutedEventArgs e)
        {
            if (ShiftsDataGrid.SelectedItem is ShiftModel selectedShift)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"Are you sure you want to delete shift \"{selectedShift.Name}\"?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            conn.Open();
                            string deleteQuery = "DELETE FROM shifts WHERE ShiftID = @ShiftID";
                            using (SqlCommand cmd = new SqlCommand(deleteQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@ShiftID", selectedShift.Id);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        LoadShifts();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error deleting shift: " + ex.Message);
                    }
                }
            }
        }
    }
}
