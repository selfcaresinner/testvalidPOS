using System;
using System.Windows;

namespace PosCore.Views
{
    public partial class CashMovementWindow : Window
    {
        public string MovementType { get; private set; } = "Entrada";
        public decimal Amount { get; private set; }
        public string Reason { get; private set; } = string.Empty;

        public CashMovementWindow()
        {
            InitializeComponent();
        }

        private void RadioType_Checked(object sender, RoutedEventArgs e)
        {
            if (RadioEntrada?.IsChecked == true)
                MovementType = "Entrada";
            else if (RadioSalida?.IsChecked == true)
                MovementType = "Salida";
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AmountBox.Text) || !decimal.TryParse(AmountBox.Text, out decimal amt) || amt <= 0)
            {
                MessageBox.Show("Ingrese un monto válido y mayor a cero.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(ReasonBox.Text))
            {
                MessageBox.Show("Ingrese un motivo para el movimiento.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Amount = amt;
            Reason = ReasonBox.Text;

            DialogResult = true;
            Close();
        }
    }
}
