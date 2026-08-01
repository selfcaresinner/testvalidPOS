using System.Windows;
using PosCore.ViewModels;

namespace PosCore.Views
{
    public partial class UsersWindow : Window
    {
        public UsersWindow(UsersViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
