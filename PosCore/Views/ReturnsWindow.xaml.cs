using System.Windows;
using PosCore.ViewModels;

namespace PosCore.Views
{
    public partial class ReturnsWindow : Window
    {
        public ReturnsWindow(ReturnsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
