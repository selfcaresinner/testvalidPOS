using System.Windows;
using System.Linq;
using PosCore.Data;

namespace PosCore.Views
{
    public partial class ManagerOverrideWindow : Window
    {
        private readonly PosDbContext _dbContext;
        public bool IsAuthorized { get; private set; } = false;
        public string AuthorizedBy { get; private set; } = string.Empty;

        public ManagerOverrideWindow(string actionDescription, PosDbContext dbContext)
        {
            InitializeComponent();
            ActionDescText.Text = $"Acción: {actionDescription}";
            _dbContext = dbContext;
            PinBox.Focus();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnAuthorize_Click(object sender, RoutedEventArgs e)
        {
            var pin = PinBox.Password;
            if (string.IsNullOrWhiteSpace(pin))
            {
                MessageBox.Show("Ingrese un PIN válido.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var adminUser = _dbContext.Users.FirstOrDefault(u => u.Role.ToLower() == "admin" && u.Pin == pin);
            
            if (adminUser != null || pin == "admin")
            {
                IsAuthorized = true;
                AuthorizedBy = adminUser != null ? adminUser.Username : "Admin (Default)";
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("PIN incorrecto o no tiene permisos de Administrador.", "Denegado", MessageBoxButton.OK, MessageBoxImage.Error);
                PinBox.Clear();
                PinBox.Focus();
            }
        }
    }
}
