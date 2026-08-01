using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NINA.Core.Utility;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics.Services {
    public static class ReverseGeocodingService {
        private static readonly HttpClient httpClient;

        static ReverseGeocodingService() {
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(4); // Strict short timeout
            httpClient.DefaultRequestHeaders.Add("User-Agent", "OCD-Plugin/1.0 (N.I.N.A)");
        }

        /// <summary>
        /// Attempts to resolve a latitude and longitude into a human-readable city/town and country string.
        /// Fails silently on network errors or timeouts, returning null.
        /// </summary>
        public static async Task<string> GetLocationNameAsync(double lat, double lon) {
            // Avoid querying default (0,0) coordinates
            if (Math.Abs(lat) < 0.0001 && Math.Abs(lon) < 0.0001) {
                return null;
            }

            try {
                // Ensure invariant culture for formatting floats to strings with periods
                string latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string lonStr = lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
                
                string url = $"https://nominatim.openstreetmap.org/reverse?lat={latStr}&lon={lonStr}&format=json&accept-language=en";

                var response = await httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode) {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var data = JObject.Parse(jsonResponse);
                    
                    var address = data["address"];
                    if (address != null) {
                        string city = (string)address["city"] 
                                   ?? (string)address["town"] 
                                   ?? (string)address["village"] 
                                   ?? (string)address["suburb"] 
                                   ?? (string)address["municipality"];
                        
                        string country = (string)address["country"];

                        if (!string.IsNullOrWhiteSpace(city) && !string.IsNullOrWhiteSpace(country)) {
                            return $"{city}, {country}";
                        } else if (!string.IsNullOrWhiteSpace(city)) {
                            return city;
                        } else if (!string.IsNullOrWhiteSpace(country)) {
                            return country;
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Debug($"[OCD] ReverseGeocodingService failed to resolve {lat},{lon}. It will gracefully fall back. Reason: {ex.Message}");
            }

            return null;
        }
    }
}
