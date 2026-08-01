using System.Printing;
using System.Windows;

namespace PosCore.Views
{
    public partial class PrinterConfigWindow : Window
    {
        public string SelectedPrinter { get; private set; } = string.Empty;
        public bool PrintLogo { get; private set; } = false;

        public PrinterConfigWindow()
        {
            InitializeComponent();
            LoadPrinters();
        }

        private void LoadPrinters()
        {
            try
            {
                var server = new LocalPrintServer();
                foreach (var queue in server.GetPrintQueues())
                {
                    PrintersCombo.Items.Add(queue.Name);
                }
            }
            catch { }
            if (PrintersCombo.Items.Count > 0)
            {
                PrintersCombo.SelectedIndex = 0;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (PrintersCombo.SelectedItem != null)
            {
                SelectedPrinter = PrintersCombo.SelectedItem?.ToString() ?? "";
                PrintLogo = PrintLogoCheck.IsChecked ?? false;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Por favor seleccione una impresora.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
