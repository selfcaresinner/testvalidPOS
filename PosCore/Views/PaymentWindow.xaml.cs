using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PosCore.Views
{
    public class PaymentEntry
    {
        public string Method { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public partial class PaymentWindow : Window
    {
        public bool IsPaid { get; private set; } = false;
        public decimal Total { get; }
        
        public ObservableCollection<PaymentEntry> Payments { get; set; } = new();

        private string _inputBuffer = "";
        private decimal _tendered = 0m;

        public PaymentWindow(decimal total)
        {
            this.KeyDown += PaymentWindow_KeyDown;

            InitializeComponent();
            Total = total;
            TotalText.Text = total.ToString("C");
            
            PaymentsList.ItemsSource = Payments;
            
            UpdateState();
        }

        
        private void PaymentWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key >= System.Windows.Input.Key.D0 && e.Key <= System.Windows.Input.Key.D9)
            {
                int val = (int)e.Key - (int)System.Windows.Input.Key.D0;
                if (_inputBuffer.Length < 8) { _inputBuffer += val.ToString(); UpdateState(); }
            }
            else if (e.Key >= System.Windows.Input.Key.NumPad0 && e.Key <= System.Windows.Input.Key.NumPad9)
            {
                int val = (int)e.Key - (int)System.Windows.Input.Key.NumPad0;
                if (_inputBuffer.Length < 8) { _inputBuffer += val.ToString(); UpdateState(); }
            }
            else if (e.Key == System.Windows.Input.Key.Back)
            {
                if (_inputBuffer.Length > 0) { _inputBuffer = _inputBuffer.Substring(0, _inputBuffer.Length - 1); UpdateState(); }
            }
            else if (e.Key == System.Windows.Input.Key.Enter)
            {
                BtnPay_Click(this, null!);
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                BtnCancel_Click(this, null!);
            }
        }

        private void UpdateState()
        {
            // Parse input buffer
            if (string.IsNullOrEmpty(_inputBuffer))
            {
                _tendered = 0;
            }
            else
            {
                if (decimal.TryParse(_inputBuffer, out decimal cents))
                {
                    _tendered = cents / 100m;
                }
            }

            TenderedText.Text = _tendered.ToString("C");

            // Calculate totals
            decimal totalPaid = Payments.Sum(p => p.Amount);
            decimal remaining = Total - totalPaid;

            if (remaining < 0) remaining = 0;
            RemainingText.Text = remaining.ToString("C");

            if (totalPaid >= Total && Total > 0)
            {
                ChangeText.Text = $"Cambio: {(totalPaid - Total).ToString("C")}";
                ChangeText.Visibility = Visibility.Visible;
            }
            else
            {
                ChangeText.Visibility = Visibility.Hidden;
            }
        }

        private void BtnNum_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content is string num)
            {
                if (_inputBuffer.Length < 8)
                {
                    _inputBuffer += num;
                    UpdateState();
                }
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            _inputBuffer = "";
            UpdateState();
        }
        
        private void BtnExact_Click(object sender, RoutedEventArgs e)
        {
            decimal totalPaid = Payments.Sum(p => p.Amount);
            decimal remaining = Total - totalPaid;
            if (remaining > 0)
            {
                _tendered = remaining;
                _inputBuffer = ((long)(remaining * 100)).ToString();
                UpdateState();
            }
        }

        private void BtnAddPayment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string method)
            {
                if (_tendered <= 0)
                {
                    // Si no hay monto ingresado, sugerir el restante
                    decimal totalPaid = Payments.Sum(p => p.Amount);
                    decimal remaining = Total - totalPaid;
                    if (remaining > 0)
                    {
                        _tendered = remaining;
                    }
                    else
                    {
                        return; // Ya está pagado
                    }
                }

                Payments.Add(new PaymentEntry { Method = method, Amount = _tendered });
                _inputBuffer = ""; // reset buffer
                UpdateState();
            }
        }

        private void BtnRemovePayment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PaymentEntry entry)
            {
                Payments.Remove(entry);
                UpdateState();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        
        public decimal TipAmount { get; private set; } = 0;
        private bool _isProcessing = false;
        
        private void BtnAddTip_Click(object sender, RoutedEventArgs e)
        {
            if (_tendered > 0)
            {
                TipAmount = _tendered;
                TipText.Text = $"Propina: {TipAmount:C}";
                TipText.Visibility = Visibility.Visible;
                _inputBuffer = "";
                UpdateState();
            }
        }

        
        private void BtnPay_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;
            
            decimal totalPaid = Payments.Sum(p => p.Amount);
            if (totalPaid < Total)
            {
                MessageBox.Show($"Faltan {(Total - totalPaid).ToString("C")} por pagar.", "Pago Incompleto", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            _isProcessing = true;
            if (sender is Button btn) { btn.IsEnabled = false; }
            
            IsPaid = true;
            DialogResult = true;
            Close();
        }

    }
}
