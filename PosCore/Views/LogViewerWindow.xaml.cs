using System.Windows;
using PosCore.ViewModels;

namespace PosCore.Views
{
    public partial class LogViewerWindow : Window
    {
        public LogViewerWindow(LogViewerViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
