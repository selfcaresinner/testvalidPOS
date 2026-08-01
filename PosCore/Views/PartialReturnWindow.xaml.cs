using System.Collections.Generic;
using System.Linq;
using System.Windows;
using PosCore.Models;

namespace PosCore.Views
{
    public class ReturnItemViewModel
    {
        public OrderItem OriginalItem { get; set; } = null!;
        public bool IsSelected { get; set; }
        public string ProductName => OriginalItem.Product.Name;
        public decimal UnitPrice => OriginalItem.UnitPrice;
        public int MaxQuantity => OriginalItem.Quantity;
        public int ReturnQuantity { get; set; }
    }

    public partial class PartialReturnWindow : Window
    {
        public List<ReturnItemViewModel> ReturnItems { get; private set; }

        public PartialReturnWindow(Order order)
        {
            InitializeComponent();
            ReturnItems = order.Items.Select(i => new ReturnItemViewModel
            {
                OriginalItem = i,
                IsSelected = false,
                ReturnQuantity = i.Quantity
            }).ToList();
            
            ItemsList.ItemsSource = ReturnItems;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            var selected = ReturnItems.Where(i => i.IsSelected).ToList();
            if (!selected.Any())
            {
                MessageBox.Show("Debe seleccionar al menos un artículo.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            foreach(var item in selected)
            {
                if(item.ReturnQuantity <= 0 || item.ReturnQuantity > item.MaxQuantity)
                {
                    MessageBox.Show($"Cantidad inválida para {item.ProductName}.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            
            DialogResult = true;
            Close();
        }
    }
}
