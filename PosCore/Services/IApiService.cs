using PosCore.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PosCore.Services;

public interface IApiService
{
    Task<List<Product>> GetProductsAsync();
    Task<List<Product>> GetChangesAsync(DateTime since);
    Task<bool> SyncProductAsync(Product product);
    Task<bool> SyncOrderAsync(Order order);
    Task<LoginResponse?> LoginAsync(string username, string password);
}
