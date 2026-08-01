using System.Windows;
using System.Windows.Controls;

namespace PosCore.Views
{
    public partial class ReasonWindow : Window
    {
        public string SelectedReason { get; private set; } = string.Empty;

        public ReasonWindow()
        {
            InitializeComponent();
            ReasonCombo.SelectionChanged += ReasonCombo_SelectionChanged;
        }

        private void ReasonCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReasonCombo.SelectedItem is ComboBoxItem item && (item.Content?.ToString() ?? "") == "Otro")
            {
                OtherReasonBox.Visibility = Visibility.Visible;
            }
            else
            {
                OtherReasonBox.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (ReasonCombo.SelectedItem is ComboBoxItem item)
            {
                SelectedReason = (item.Content?.ToString() ?? "") == "Otro" ? OtherReasonBox.Text : (item.Content?.ToString() ?? "");
            }

            if (string.IsNullOrWhiteSpace(SelectedReason))
            {
                MessageBox.Show("Por favor, ingrese o seleccione un motivo.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}
