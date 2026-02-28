using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VeterinarianEMS.Models;
using VeterinarianEMS.Controls;

namespace VeterinarianEMS
{
    public partial class RolesControl : UserControl
    {
        private string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True;";

        // Master lists
        private List<PositionModel> _allPositions = new List<PositionModel>();
        private List<DepartmentModel> _allDepartments = new List<DepartmentModel>();

        // Filtered lists
        private List<PositionModel> _filteredPositions = new List<PositionModel>();
        private List<DepartmentModel> _filteredDepartments = new List<DepartmentModel>();

        // Pagination
        private int _positionPage = 1;
        private int _departmentPage = 1;
        private int _positionPageSize = 5;
        private int _departmentPageSize = 5;
        private int _positionTotalPages = 1;
        private int _departmentTotalPages = 1;

        public RolesControl()
        {
            InitializeComponent();

            // Subscribe to entries selectors
            PositionEntriesSelector.EntriesChanged += PositionEntriesSelector_EntriesChanged;
            DepartmentEntriesSelector.EntriesChanged += DepartmentEntriesSelector_EntriesChanged;

            LoadPositions();
            LoadDepartments();
        }

        // ---------------- POSITIONS ----------------
        private void LoadPositions()
        {
            try
            {
                _allPositions.Clear();
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT PositionID, PositionName FROM empositions ORDER BY PositionID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _allPositions.Add(new PositionModel
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }

                ApplyPositionFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading Positions: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyPositionFilter(string search = "")
        {
            _filteredPositions = string.IsNullOrWhiteSpace(search)
                ? _allPositions
                : _allPositions
                    .Where(x => x.Name.ToLower().Contains(search.ToLower())
                             || x.Id.ToString().Contains(search))
                    .ToList();

            _positionPage = 1;
            LoadPositionsPage();
        }

        private void LoadPositionsPage()
        {
            if (_filteredPositions.Count == 0)
            {
                PositionDataGrid.ItemsSource = null;
                UpdatePositionPageInfo();
                return;
            }

            _positionTotalPages = (int)Math.Ceiling((double)_filteredPositions.Count / _positionPageSize);
            _positionPage = Math.Min(Math.Max(_positionPage, 1), _positionTotalPages);

            var pageData = _filteredPositions
                .Skip((_positionPage - 1) * _positionPageSize)
                .Take(_positionPageSize)
                .ToList();

            PositionDataGrid.ItemsSource = pageData;
            UpdatePositionPageInfo();
        }

        private void PositionEntriesSelector_EntriesChanged(object sender, int entries)
        {
            _positionPageSize = entries;
            _positionPage = 1;
            LoadPositionsPage();
        }

        private void PositionSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyPositionFilter(((TextBox)sender).Text);
        }

        private void PositionPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_positionPage > 1)
            {
                _positionPage--;
                LoadPositionsPage();
            }
        }

        private void PositionNext_Click(object sender, RoutedEventArgs e)
        {
            if (_positionPage < _positionTotalPages)
            {
                _positionPage++;
                LoadPositionsPage();
            }
        }

        private void UpdatePositionPageInfo()
        {
            if (PositionPageNumber != null)
                PositionPageNumber.Text = _positionPage.ToString();

            if (PositionShowingText != null)
            {
                PositionShowingText.Text =
                    $"Showing {_positionPage} to {_positionTotalPages} of {_filteredPositions.Count} entries";
            }
        }


        // ---------------- DEPARTMENTS ----------------
        private void LoadDepartments()
        {
            try
            {
                _allDepartments.Clear();
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT DepartmentID, DepartmentName FROM department ORDER BY DepartmentID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _allDepartments.Add(new DepartmentModel
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }

                ApplyDepartmentFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading Departments: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyDepartmentFilter(string search = "")
        {
            _filteredDepartments = string.IsNullOrWhiteSpace(search)
                ? _allDepartments
                : _allDepartments
                    .Where(x => x.Name.ToLower().Contains(search.ToLower())
                             || x.Id.ToString().Contains(search))
                    .ToList();

            _departmentPage = 1;
            LoadDepartmentsPage();
        }

        private void LoadDepartmentsPage()
        {
            if (_filteredDepartments.Count == 0)
            {
                DepartmentDataGrid.ItemsSource = null;
                UpdateDepartmentPageInfo();
                return;
            }

            _departmentTotalPages = (int)Math.Ceiling((double)_filteredDepartments.Count / _departmentPageSize);
            _departmentPage = Math.Min(Math.Max(_departmentPage, 1), _departmentTotalPages);

            var pageData = _filteredDepartments
                .Skip((_departmentPage - 1) * _departmentPageSize)
                .Take(_departmentPageSize)
                .ToList();

            DepartmentDataGrid.ItemsSource = pageData;
            UpdateDepartmentPageInfo();
        }

        private void DepartmentEntriesSelector_EntriesChanged(object sender, int entries)
        {
            _departmentPageSize = entries;
            _departmentPage = 1;
            LoadDepartmentsPage();
        }

        private void DepartmentSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyDepartmentFilter(((TextBox)sender).Text);
        }

        private void DepartmentPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_departmentPage > 1)
            {
                _departmentPage--;
                LoadDepartmentsPage();
            }
        }

        private void DepartmentNext_Click(object sender, RoutedEventArgs e)
        {
            if (_departmentPage < _departmentTotalPages)
            {
                _departmentPage++;
                LoadDepartmentsPage();
            }
        }

        private void UpdateDepartmentPageInfo()
        {
            if (DepartmentPageNumber != null)
                DepartmentPageNumber.Text = _departmentPage.ToString();

            if (DepartmentShowingText != null)
            {
                DepartmentShowingText.Text =
                    $"Showing {_departmentPage} to {_departmentTotalPages} of {_filteredDepartments.Count} entries";
            }
        }


        // ---------------- CRUD POPUPS ----------------
        private void AddPosition_Click(object sender, RoutedEventArgs e)
        {
            PositionPopup popup = new PositionPopup
            {
                TableName = "empositions",
                FieldName = "PositionName",
                TitleLabel = "Position Name:"
            };

            popup.OnClose += (saved) =>
            {
                if (saved) LoadPositions();
            };

            // Create hosting window with fade-in
            var popupWindow = new Window
            {
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.Transparent,
                AllowsTransparency = true,
                ShowInTaskbar = false,
                Content = popup,
                Topmost = true
            };

            // Fade-in animation
            popupWindow.Opacity = 0;
            popupWindow.Loaded += (s, e2) =>
            {
                var fade = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
                popupWindow.BeginAnimation(Window.OpacityProperty, fade);
            };

            popupWindow.ShowDialog();
        }

        private void EditPosition_Click(object sender, RoutedEventArgs e)
        {
            if (PositionDataGrid.SelectedItem is PositionModel pos)
            {
                // Open popup for editing
                PositionPopup popup = new PositionPopup
                {
                    TableName = "empositions",       // Table to update
                    FieldName = "PositionName",      // Column to update
                    TitleLabel = "Position Name:",   // Label in popup
                    EditingId = pos.Id               // ID of the position being edited
                };
                popup.PositionNameTextBox.Text = pos.Name;

                popup.OnClose += (saved) =>
                {
                    if (saved)
                    {
                        // Reload DataGrid after editing
                        LoadPositions();
                    }
                };

                // Create hosting window with fade-in
                var popupWindow = new Window
                {
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(this),
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.NoResize,
                    Background = System.Windows.Media.Brushes.Transparent,
                    AllowsTransparency = true,
                    ShowInTaskbar = false,
                    Content = popup,
                    Topmost = true
                };

                // Fade-in animation
                popupWindow.Opacity = 0;
                popupWindow.Loaded += (s, e2) =>
                {
                    var fade = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
                    popupWindow.BeginAnimation(Window.OpacityProperty, fade);
                };

                popupWindow.ShowDialog();
            }
        }

        private void DeletePosition_Click(object sender, RoutedEventArgs e)
        {
            if (PositionDataGrid.SelectedItem is PositionModel pos)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete Position '{pos.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            conn.Open();
                            string query = "DELETE FROM empositions WHERE PositionID = @Id";

                            using (SqlCommand cmd = new SqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@Id", pos.Id);
                                int rowsAffected = cmd.ExecuteNonQuery();

                                if (rowsAffected == 0)
                                    MessageBox.Show("Position not found or already deleted.");
                            }
                        }

                        // Refresh DataGrid
                        LoadPositions();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting Position: {ex.Message}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void AddDepartment_Click(object sender, RoutedEventArgs e)
        {
            PositionPopup popup = new PositionPopup
            {
                TableName = "department",
                FieldName = "DepartmentName",
                TitleLabel = "Department Name:"
            };

            popup.OnClose += (saved) =>
            {
                if (saved) LoadDepartments();
            };

            // Create hosting window with fade-in
            var popupWindow = new Window
            {
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.Transparent,
                AllowsTransparency = true,
                ShowInTaskbar = false,
                Content = popup,
                Topmost = true
            };

            // Fade-in animation
            popupWindow.Opacity = 0;
            popupWindow.Loaded += (s, e2) =>
            {
                var fade = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
                popupWindow.BeginAnimation(Window.OpacityProperty, fade);
            };

            popupWindow.ShowDialog();
        }

        private void EditDepartment_Click(object sender, RoutedEventArgs e)
        {
            if (DepartmentDataGrid.SelectedItem is DepartmentModel dep)
            {
                PositionPopup popup = new PositionPopup
                {
                    TableName = "department",         // the table to edit
                    FieldName = "DepartmentName",     // the column to update
                    IdColumn = "DepartmentID",        // ✅ set the correct primary key column
                    TitleLabel = "Department Name:",
                    EditingId = dep.Id
                };

                popup.PositionNameTextBox.Text = dep.Name;

                popup.OnClose += (saved) =>
                {
                    if (saved) LoadDepartments();
                };

                // Create hosting window with fade-in
                var popupWindow = new Window
                {
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(this),
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.NoResize,
                    Background = System.Windows.Media.Brushes.Transparent,
                    AllowsTransparency = true,
                    ShowInTaskbar = false,
                    Content = popup,
                    Topmost = true
                };

                // Fade-in animation
                popupWindow.Opacity = 0;
                popupWindow.Loaded += (s, e2) =>
                {
                    var fade = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
                    popupWindow.BeginAnimation(Window.OpacityProperty, fade);
                };

                popupWindow.ShowDialog();
            }
        }

        private void DeleteDepartment_Click(object sender, RoutedEventArgs e)
        {
            if (DepartmentDataGrid.SelectedItem is DepartmentModel dep)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete Department '{dep.Name}'?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            conn.Open();
                            string query = "DELETE FROM department WHERE DepartmentID = @Id";

                            using (SqlCommand cmd = new SqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@Id", dep.Id);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        LoadDepartments();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting Department: {ex.Message}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // ---------------- HELPER ----------------
        private void ShowPopup(UserControl popupControl)
        {
            Window popupWindow = new Window
            {
                Title = "",
                Content = popupControl,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                Owner = Window.GetWindow(this),
                Background = null,
                AllowsTransparency = true
            };
            popupWindow.ShowDialog();
        }
    }
}
