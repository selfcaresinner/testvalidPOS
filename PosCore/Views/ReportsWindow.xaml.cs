using System.Windows;
using PosCore.ViewModels;

namespace PosCore.Views;

public partial class ReportsWindow : Window
{
    public ReportsWindow(ReportsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
