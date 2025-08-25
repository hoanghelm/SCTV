using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;

namespace PersonDetections.Service.Services;

public interface IFirebaseNotificationService
{
    Task<bool> SendNotificationAsync(string title, string body, string deviceToken = null);
    Task<bool> SendPersonDetectionNotificationAsync(string cameraName, int detectionCount, string deviceToken = null);
}

public class FirebaseNotificationService : IFirebaseNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FirebaseNotificationService> _logger;
    private readonly string _projectId;
    private readonly string _privateKey;
    private readonly string _clientEmail;
    private readonly string _fcmUrl;

    public FirebaseNotificationService(HttpClient httpClient, ILogger<FirebaseNotificationService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _projectId = configuration["Firebase:ProjectId"] ?? Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID");
        _privateKey = configuration["Firebase:PrivateKey"] ?? Environment.GetEnvironmentVariable("FIREBASE_PRIVATE_KEY");
        _clientEmail = configuration["Firebase:ClientEmail"] ?? Environment.GetEnvironmentVariable("FIREBASE_CLIENT_EMAIL");
        
        _fcmUrl = $"https://fcm.googleapis.com/v1/projects/{_projectId}/messages:send";
        
        if (string.IsNullOrEmpty(_projectId) || string.IsNullOrEmpty(_privateKey) || string.IsNullOrEmpty(_clientEmail))
        {
            _logger.LogWarning("Firebase credentials not fully configured. ProjectId: {ProjectId}, PrivateKey: {HasPrivateKey}, ClientEmail: {ClientEmail}", 
                _projectId, !string.IsNullOrEmpty(_privateKey), _clientEmail);
        }
    }

    public async Task<bool> SendNotificationAsync(string title, string body, string deviceToken = null)
    {
        try
        {
            if (string.IsNullOrEmpty(_projectId) || string.IsNullOrEmpty(_privateKey) || string.IsNullOrEmpty(_clientEmail))
            {
                _logger.LogError("Firebase credentials are not configured");
                return false;
            }

            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("Failed to get access token");
                return false;
            }

            var message = new
            {
                message = new
                {
                    topic = "person_detection",
                    notification = new
                    {
                        title = title,
                        body = body
                    },
                    data = new Dictionary<string, string>
                    {
                        { "type", "person_detection" },
                        { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }
                    },
                    android = new
                    {
                        notification = new
                        {
                            icon = "ic_launcher",
                            sound = "default"
                        }
                    }
                }
            };

            var jsonPayload = JsonSerializer.Serialize(message);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.PostAsync(_fcmUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("FCM notification sent successfully. Response: {Response}", responseContent);
                return true;
            }
            else
            {
                _logger.LogError("Failed to send FCM notification. Status: {Status}, Response: {Response}", 
                    response.StatusCode, responseContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending FCM notification");
            return false;
        }
    }

    public async Task<bool> SendPersonDetectionNotificationAsync(string cameraName, int detectionCount, string deviceToken = null)
    {
        var title = "Person Detected";
        var body = $"{detectionCount} person(s) detected on {cameraName}";
        
        return await SendNotificationAsync(title, body, deviceToken);
    }

    private async Task<string> GetAccessTokenAsync()
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            
            // Create JWT header
            var header = new { alg = "RS256", typ = "JWT" };
            var headerJson = JsonSerializer.Serialize(header);
            var headerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerJson)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            
            // Create JWT payload
            var payload = new
            {
                iss = _clientEmail,
                sub = _clientEmail,
                aud = "https://oauth2.googleapis.com/token",
                iat = now.ToUnixTimeSeconds(),
                exp = now.AddHours(1).ToUnixTimeSeconds(),
                scope = "https://www.googleapis.com/auth/firebase.messaging"
            };
            var payloadJson = JsonSerializer.Serialize(payload);
            var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            
            // Create signature
            var signatureInput = $"{headerBase64}.{payloadBase64}";
            var signatureInputBytes = Encoding.UTF8.GetBytes(signatureInput);
            
            var privateKeyBytes = Convert.FromBase64String(_privateKey.Replace("-----BEGIN PRIVATE KEY-----", "").Replace("-----END PRIVATE KEY-----", "").Replace("\n", "").Replace("\r", ""));
            using var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
            
            var signatureBytes = rsa.SignData(signatureInputBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var signatureBase64 = Convert.ToBase64String(signatureBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            
            var jwt = $"{signatureInput}.{signatureBase64}";

            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
                new KeyValuePair<string, string>("assertion", jwt)
            });

            var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", tokenRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                return tokenResponse.GetProperty("access_token").GetString();
            }
            else
            {
                _logger.LogError("Failed to get access token. Response: {Response}", responseContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting access token");
            return null;
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            _logger.LogInformation("Testing FCM connection with project: {ProjectId}", _projectId);
            
            var accessToken = await GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(accessToken))
            {
                _logger.LogInformation("Successfully obtained access token");
                return true;
            }
            else
            {
                _logger.LogError("Failed to obtain access token");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing FCM connection");
            return false;
        }
    }
}