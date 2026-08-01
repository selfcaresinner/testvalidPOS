using System.Windows;
using PosCore.ViewModels;

namespace PosCore.Views
{
    public partial class ShiftWindow : Window
    {
        public ShiftWindow(ShiftViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
