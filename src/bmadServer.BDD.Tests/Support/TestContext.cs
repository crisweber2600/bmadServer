using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace bmadServer.BDD.Tests.Support
{
    /// <summary>
    /// Shared test context for BDD tests
    /// Manages API client, database connections, and test data
    /// </summary>
    public class TestContext : IAsyncDisposable
    {
        private HttpClient? _httpClient;
        private readonly string _apiBaseUrl = "http://localhost:8080";
        
        public HttpClient ApiClient => _httpClient ??= new HttpClient { BaseAddress = new Uri(_apiBaseUrl) };
        
        // Store test user data
        public string? LastUserEmail { get; set; }
        public string? LastUserPassword { get; set; }
        public string? LastAccessToken { get; set; }
        public string? LastRefreshToken { get; set; }
        public Guid LastUserId { get; set; }
        public List<HttpResponseMessage>? ConcurrentResponses { get; set; }
        
        public TestContext()
        {
            InitializeHttpClient();
        }
        
        private void InitializeHttpClient()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_apiBaseUrl),
                Timeout = TimeSpan.FromSeconds(10)
            };
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }
        
        /// <summary>
        /// Set authorization header with Bearer token
        /// </summary>
        public void SetAuthorizationToken(string token)
        {
            if (_httpClient != null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }
        
        /// <summary>
        /// Clear authorization header
        /// </summary>
        public void ClearAuthorizationToken()
        {
            if (_httpClient != null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }
        
        public async ValueTask DisposeAsync()
        {
            _httpClient?.Dispose();
            await Task.CompletedTask;
        }
    }
}
