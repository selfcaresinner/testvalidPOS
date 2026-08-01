using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using PosCore.Models;

namespace PosCore.Views
{
    public partial class SuspendedOrdersWindow : Window
    {
        public ObservableCollection<OrderItem>? SelectedOrder { get; private set; }

        public SuspendedOrdersWindow(ObservableCollection<ObservableCollection<OrderItem>> suspendedOrders)
        {
            InitializeComponent();
            OrdersList.ItemsSource = suspendedOrders;
        }

        private void BtnResume_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ObservableCollection<OrderItem> order)
            {
                SelectedOrder = order;
                DialogResult = true;
                Close();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
