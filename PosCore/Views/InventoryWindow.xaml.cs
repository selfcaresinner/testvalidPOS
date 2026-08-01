using System.Windows;
using PosCore.ViewModels;

namespace PosCore.Views;

public partial class InventoryWindow : Window
{
    public InventoryWindow(InventoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
