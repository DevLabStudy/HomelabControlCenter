using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace HomelabControlCenter
{
    public sealed class OrbitApiClient : IDisposable
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private string _baseUrl = "http://localhost:5001";
        private string _token = string.Empty;

        public void Configure(string baseUrl)
        {
            _baseUrl = baseUrl.Trim().TrimEnd('/');
        }

        public async Task LoginAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            using var content = new StringContent(
                JsonSerializer.Serialize(new { email, password }),
                Encoding.UTF8,
                "application/json");
            using var response = await _httpClient.PostAsync(
                $"{_baseUrl}/api/v1/auth/login",
                content,
                cancellationToken);
            await EnsureSuccessAsync(response);

            var result = await ReadAsync<OrbitLoginResponse>(response, cancellationToken);
            _token = result.Token ?? string.Empty;
            if (string.IsNullOrWhiteSpace(_token))
                throw new InvalidOperationException("Orbit nie zwrócił tokenu API.");
        }

        public async Task<IReadOnlyList<OrbitContainer>> GetContainersAsync(CancellationToken cancellationToken = default)
        {
            using var response = await SendAsync(HttpMethod.Get, "/api/v1/containers", cancellationToken: cancellationToken);
            await EnsureSuccessAsync(response);
            var result = await ReadAsync<OrbitContainersResponse>(response, cancellationToken);
            return result.Containers ?? new List<OrbitContainer>();
        }

        public async Task ExecuteContainerActionAsync(string id, string action, CancellationToken cancellationToken = default)
        {
            using var response = await SendAsync(
                HttpMethod.Post,
                $"/api/v1/containers/{Uri.EscapeDataString(id)}/{Uri.EscapeDataString(action)}",
                cancellationToken: cancellationToken);
            await EnsureSuccessAsync(response);
        }

        private async Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            string path,
            HttpContent? content = null,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(method, $"{_baseUrl}{path}") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            return await _httpClient.SendAsync(request, cancellationToken);
        }

        private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Orbit zwrócił pustą odpowiedź.");
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return;
            var body = await response.Content.ReadAsStringAsync();
            try
            {
                var error = JsonSerializer.Deserialize<OrbitError>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                throw new InvalidOperationException(error?.Message ?? $"Orbit HTTP {(int)response.StatusCode}.");
            }
            catch (JsonException)
            {
                throw new InvalidOperationException($"Orbit HTTP {(int)response.StatusCode}: {body}");
            }
        }

        public void Dispose() => _httpClient.Dispose();
    }

    public sealed class OrbitLoginResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }

    public sealed class OrbitContainersResponse
    {
        [JsonPropertyName("containers")]
        public List<OrbitContainer>? Containers { get; set; }
    }

    public sealed class OrbitContainer
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public List<OrbitPort> Ports { get; set; } = new List<OrbitPort>();
    }

    public sealed class OrbitPort
    {
        public string Container { get; set; } = string.Empty;
        public string? Host { get; set; }
        public string? Url { get; set; }
    }

    public sealed class OrbitError
    {
        public string? Message { get; set; }
    }
}
