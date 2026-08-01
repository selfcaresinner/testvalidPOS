using System.Windows;
using PosCore.ViewModels;

namespace PosCore.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose = () => {
            this.DialogResult = true;
            this.Close();
        };
    }
}
