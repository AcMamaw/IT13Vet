using Microsoft.Data.SqlClient;
using System;
using System.Data.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace VeterinarianEMS.Controls
{
    public partial class PositionPopup : UserControl
    {
        public event Action<bool> OnClose;

        private string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=VeterinarianEMS;Integrated Security=True;";

        // Configurable properties
        public string TableName { get; set; } = "empositions";   // default table
        public string FieldName { get; set; } = "PositionName";  // default column
        public int? EditingId { get; set; } = null;
        // The primary key column of the table, e.g., "PositionID" or "DepartmentID"
        public string IdColumn { get; set; } = "PositionID"; // default to PositionID
                                                             // null = insert, value = update

        public string TitleLabel
        {
            get => LabelBlock.Text;
            set => LabelBlock.Text = value;
        }

        public PositionPopup()
        {
            InitializeComponent();
        }

        // Run animation when popup loads
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (Resources["PopupShowAnimation"] is Storyboard sb)
            {
                sb.Begin();
            }
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string name = PositionNameTextBox.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show($"Please enter a {FieldName.Replace("Name", "")} name.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query;

                    if (EditingId.HasValue)
                    {
                        // ✅ Update existing row
                        query = $"UPDATE {TableName} SET {FieldName} = @Name WHERE {IdColumn} = @Id";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@Name", name);
                            cmd.Parameters.AddWithValue("@Id", EditingId.Value);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show($"{FieldName.Replace("Name", "")} updated successfully!",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // ✅ Insert new row
                        query = $"INSERT INTO {TableName} ({FieldName}) VALUES (@Name)";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@Name", name);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show($"{FieldName.Replace("Name", "")} saved successfully!",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                OnClose?.Invoke(true);

                if (Application.Current.MainWindow is MainWindow main)
                {
                    main.RefreshData();
                }

                CloseParentWindow();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving {FieldName.Replace("Name", "")}: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            OnClose?.Invoke(false);
            CloseParentWindow();
        }

        private void CloseParentWindow()
        {
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.Close();
            }
        }
    }
}
