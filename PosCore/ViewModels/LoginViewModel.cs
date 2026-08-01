using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosCore.Models;
using PosCore.Services;
using System;
using System.Threading.Tasks;

namespace PosCore.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IApiService _apiService;
    private readonly SessionManager _sessionManager;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public Action? RequestClose { get; set; }

    private readonly PosCore.Data.PosDbContext _dbContext;

    public LoginViewModel(IApiService apiService, SessionManager sessionManager, PosCore.Data.PosDbContext dbContext)
    {
        _apiService = apiService;
        _sessionManager = sessionManager;
        _dbContext = dbContext;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Ingrese usuario y contraseña";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        // 1. Try API first
        try
        {
            var result = await _apiService.LoginAsync(Username, Password);
            if (result != null && !string.IsNullOrEmpty(result.Token))
            {
                _sessionManager.Token = result.Token;
                _sessionManager.CurrentTenantId = string.IsNullOrEmpty(result.TenantId) ? "default" : result.TenantId;
                _sessionManager.Username = Username;
                _sessionManager.Role = string.IsNullOrEmpty(result.Role) ? "User" : result.Role;
                _sessionManager.SaveSession();

                // Save or update local user so offline works next time
                var existingUser = Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.IgnoreQueryFilters(_dbContext.Users).FirstOrDefault(u => u.Username.ToLower() == Username.ToLower());
                if (existingUser != null)
                {
                    existingUser.Pin = Password;
                    existingUser.Role = _sessionManager.Role;
                    existingUser.TenantId = _sessionManager.CurrentTenantId;
                }
                else
                {
                    _dbContext.Users.Add(new PosCore.Models.User
                    {
                        Username = Username,
                        Pin = Password,
                        Role = _sessionManager.Role,
                        TenantId = _sessionManager.CurrentTenantId,
                        IsActive = true,
                        CreatedAt = System.DateTime.Now
                    });
                }
                _dbContext.SaveChanges();

                RequestClose?.Invoke();
                IsLoading = false;
                return;
            }
        }
        catch { /* Fallback to local */ }

        // 2. Fallback: check local database
        var localUser = Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.IgnoreQueryFilters(_dbContext.Users).FirstOrDefault(u => u.Username.ToLower() == Username.ToLower() && u.Pin == Password);
        if (localUser != null)
        {
            _sessionManager.Token = "local-token-" + Guid.NewGuid().ToString();
            _sessionManager.CurrentTenantId = string.IsNullOrEmpty(localUser.TenantId) ? "default" : localUser.TenantId;
            _sessionManager.Username = localUser.Username;
            _sessionManager.Role = localUser.Role;
            _sessionManager.SaveSession();
            RequestClose?.Invoke();
            IsLoading = false;
            return;
        }

        // 3. Default admin if no users exist
        if (!Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.IgnoreQueryFilters(_dbContext.Users).Any() && Username.ToLower() == "admin" && Password == "admin")
        {
            _sessionManager.Token = "local-token-admin";
            _sessionManager.CurrentTenantId = "default";
            _sessionManager.Username = "Admin";
            _sessionManager.Role = "Admin";
            _sessionManager.SaveSession();
            RequestClose?.Invoke();
            IsLoading = false;
            return;
        }

        ErrorMessage = "No se pudo iniciar sesión. Verifique sus credenciales o la conexión con el servidor.";
        IsLoading = false;
    }
}
