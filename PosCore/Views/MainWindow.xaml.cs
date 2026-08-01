using System.Windows;
using PosCore.ViewModels;

namespace PosCore.Views;

public partial class MainWindow : Window
{
        private void TestModifiers_Click(object sender, RoutedEventArgs e)
        {
            var testWindow = new TestModifiersWindow();
            testWindow.ShowDialog();
        }

    private void SearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.ProcessBarcode();
                }
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tb)
            {
                tb.SelectAll();
            }
        }

        public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        
        // Asignamos el ViewModel al DataContext para que los Bindings de XAML funcionen
        DataContext = viewModel;
    }
}
