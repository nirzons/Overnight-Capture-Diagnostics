using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NirZonshine.NINA.OvernightCaptureDiagnostics.Models;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics.Services {
    public class WebhookService {
        private static readonly HttpClient HttpClient = new HttpClient();

        public async Task PostDiscordSummary(string webhookUrl, SessionData session) {
            if (string.IsNullOrWhiteSpace(webhookUrl)) return;

            try {
                var payload = new {
                    username = "Overnight Capture Diagnostics",
                    avatar_url = "https://raw.githubusercontent.com/isbeorn/NINA/main/NINA/Resources/Images/Telescope.png",
                    embeds = new[] {
                        new {
                            title = $"🔭 OCD Session Report — {session.SessionStart:yyyy-MM-dd}",
                            color = session.EmergencySafetyAbort ? 15158332 : 3066993,
                            fields = new[] {
                                new { name = "🌟 Night Score", value = $"{session.MasterQualityScore:F0} / 100", inline = true },
                                new { name = "⏱️ Integration", value = $"{(int)session.TotalNightIntegrationSeconds / 3600}h {((int)session.TotalNightIntegrationSeconds % 3600) / 60}m", inline = true },
                                new { name = "📈 Efficiency", value = $"{session.ImagingEfficiencyPercent:F1}%", inline = true },
                                new { name = "🎯 Targets", value = $"{session.Targets.Count} Target(s)", inline = true }
                            },
                            footer = new { text = "N.I.N.A. OCD Plugin" },
                            timestamp = DateTime.UtcNow.ToString("o")
                        }
                    }
                };

                string json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await HttpClient.PostAsync(webhookUrl, content);
            } catch {
                // Silently ignore webhook failure
            }
        }
    }
}
