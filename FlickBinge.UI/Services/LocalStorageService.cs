using System.Text.Json;
using Microsoft.JSInterop;

namespace FlickBinge.UI.Services
{
    public class LocalStorageService
    {
        private readonly IJSRuntime _js;

        public LocalStorageService(IJSRuntime js)
        {
            _js = js;
        }

        // Store plain string
        public async Task SetItemAsync(string key, string value) =>
            await _js.InvokeVoidAsync("localStorage.setItem", key, value);

        // Get plain string
        public async Task<string?> GetItemAsync(string key) =>
            await _js.InvokeAsync<string>("localStorage.getItem", key);

        // Remove item
        public async Task RemoveItemAsync(string key) =>
            await _js.InvokeVoidAsync("localStorage.removeItem", key);

        // Store object as JSON
        public async Task SetItemAsync<T>(string key, T value)
        {
            var json = JsonSerializer.Serialize(value);
            await _js.InvokeVoidAsync("localStorage.setItem", key, json);
        }

        // Get object from JSON
        public async Task<T?> GetItemAsync<T>(string key)
        {
            var json = await _js.InvokeAsync<string>("localStorage.getItem", key);
            return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json);
        }
    }
}
