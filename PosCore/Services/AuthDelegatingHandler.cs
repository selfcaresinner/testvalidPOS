using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace PosCore.Services;

public class AuthDelegatingHandler : DelegatingHandler
{
    private readonly SessionManager _sessionManager;

    public AuthDelegatingHandler(SessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_sessionManager.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _sessionManager.Token);
        }

        if (!string.IsNullOrWhiteSpace(_sessionManager.CurrentTenantId))
        {
            if (request.Headers.Contains("X-Tenant-Id"))
                request.Headers.Remove("X-Tenant-Id");

            request.Headers.Add("X-Tenant-Id", _sessionManager.CurrentTenantId);
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _sessionManager.ClearSession();
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var loginWindow = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<PosCore.Views.LoginWindow>(PosCore.App.ServiceProvider!);
                System.Windows.Application.Current.MainWindow = loginWindow;
                loginWindow.Show();
                
                // Cierra la ventana principal si sigue abierta
                foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
                {
                    if (window != loginWindow)
                    {
                        window.Close();
                    }
                }
            });
        }
        return response;
    }
}
