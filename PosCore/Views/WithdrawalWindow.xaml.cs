using System.Windows;

namespace PosCore.Views
{
    public partial class WithdrawalWindow : Window
    {
        public decimal Amount { get; private set; }
        public string Reason { get; private set; } = string.Empty;

        public WithdrawalWindow()
        {
            InitializeComponent();
            AmountBox.Focus();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(AmountBox.Text, out decimal amt) && amt > 0)
            {
                Amount = amt;
                Reason = ReasonBox.Text;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Monto inválido.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
