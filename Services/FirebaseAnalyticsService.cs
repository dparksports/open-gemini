using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OpenClaw.Windows.Services
{
    public class FirebaseAnalyticsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FirebaseAnalyticsService> _logger;
        private string? _measurementId;
        private string? _apiSecret;
        private string _clientId;

        // Session management
        private string _sessionId = string.Empty;
        private DateTime _lastActivityTime;
        private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(30);

        private const string BaseUrl = "https://www.google-analytics.com/mp/collect";

        public FirebaseAnalyticsService(ILogger<FirebaseAnalyticsService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _clientId = GetOrCreateClientId();
            LoadConfiguration();
            RestoreSession();
        }

        private void LoadConfiguration()
        {
            try
            {
                var configFile = System.IO.Path.Combine(AppContext.BaseDirectory, "firebase_config.json");
                if (System.IO.File.Exists(configFile))
                {
                    var json = System.IO.File.ReadAllText(configFile);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    
                    if (root.TryGetProperty("measurementId", out var mId))
                    {
                        _measurementId = mId.GetString();
                    }
                    if (root.TryGetProperty("apiSecret", out var secret))
                    {
                        _apiSecret = secret.GetString();
                    }
                }
                else
                {
                    _logger.LogWarning("firebase_config.json not found.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Firebase configuration.");
            }
        }

        private string GetOrCreateClientId()
        {
            var id = SettingsHelper.Get<string>("FirebaseClientId", null!);
            if (!string.IsNullOrEmpty(id))
            {
                return id;
            }

            var newId = Guid.NewGuid().ToString();
            SettingsHelper.Set("FirebaseClientId", newId);
            return newId;
        }

        /// <summary>
        /// Restores or creates a new session. A session expires after 30 minutes of inactivity.
        /// The session_id is a Unix timestamp (seconds) of when the session started.
        /// </summary>
        private void RestoreSession()
        {
            var savedSessionId = SettingsHelper.Get<string>("GA4SessionId", null!);
            var savedLastActivity = SettingsHelper.Get<string>("GA4LastActivityTime", null!);

            DateTime lastActivity = DateTime.MinValue;
            if (!string.IsNullOrEmpty(savedLastActivity) &&
                long.TryParse(savedLastActivity, out var ticks))
            {
                lastActivity = new DateTime(ticks, DateTimeKind.Utc);
            }

            bool sessionExpired = string.IsNullOrEmpty(savedSessionId) ||
                                  (DateTime.UtcNow - lastActivity) > SessionTimeout;

            if (sessionExpired)
            {
                StartNewSession();
            }
            else
            {
                _sessionId = savedSessionId!;
                _lastActivityTime = lastActivity;
            }
        }

        /// <summary>
        /// Generates a new session ID (Unix epoch seconds) and persists it.
        /// </summary>
        private void StartNewSession()
        {
            _sessionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            _lastActivityTime = DateTime.UtcNow;
            SettingsHelper.Set("GA4SessionId", _sessionId);
            SettingsHelper.Set("GA4LastActivityTime", _lastActivityTime.Ticks.ToString());
            _logger.LogInformation("Started new GA4 session: {SessionId}", _sessionId);
        }

        /// <summary>
        /// Refreshes the session — starts a new one if 30 min have elapsed since last activity.
        /// </summary>
        private void RefreshSession()
        {
            if ((DateTime.UtcNow - _lastActivityTime) > SessionTimeout)
            {
                StartNewSession();
            }
            else
            {
                _lastActivityTime = DateTime.UtcNow;
                SettingsHelper.Set("GA4LastActivityTime", _lastActivityTime.Ticks.ToString());
            }
        }

        public async Task LogEventAsync(string eventName, object? parsedParams = null)
        {
            // Check Opt-out
            var enabled = SettingsHelper.Get<bool>("AnalyticsEnabled", true);
            if (!enabled) return;

            if (string.IsNullOrEmpty(_measurementId) || string.IsNullOrEmpty(_apiSecret))
            {
                // Config missing, skip
                return;
            }

            try
            {
                // Refresh session (handles 30-min timeout)
                RefreshSession();

                // Build merged params with session_id and engagement_time_msec
                var mergedParams = BuildParams(parsedParams);

                var payload = new
                {
                    client_id = _clientId,
                    events = new[]
                    {
                        new
                        {
                            name = eventName,
                            @params = mergedParams
                        }
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var url = $"{BaseUrl}?measurement_id={_measurementId}&api_secret={_apiSecret}";

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to send analytics event {Event}. Status: {Status}", eventName, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending analytics event {Event}", eventName);
            }
        }

        /// <summary>
        /// Merges caller-provided params with mandatory GA4 session fields.
        /// Ensures every event carries session_id and engagement_time_msec.
        /// </summary>
        private Dictionary<string, object> BuildParams(object? callerParams)
        {
            var merged = new Dictionary<string, object>();

            // Deserialize caller params if provided
            if (callerParams != null)
            {
                try
                {
                    var json = JsonSerializer.Serialize(callerParams);
                    var deserialized = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    if (deserialized != null)
                    {
                        foreach (var kvp in deserialized)
                        {
                            merged[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize caller params, using defaults only.");
                }
            }

            // Inject mandatory session params (always override to ensure correctness)
            merged["session_id"] = _sessionId;
            merged["engagement_time_msec"] = "100";

            return merged;
        }
    }
}
