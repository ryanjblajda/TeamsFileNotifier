using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TeamsFileNotifier.Notifications
{
    internal class TeamsNotifier
    {
        public void Notify(string message, string webhookUrl)
        {
            var payload = new { text = message };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                //var response = await HttpClient.PostAsync(webhookUrl, content);
                //if (!response.IsSuccessStatusCode)
                    //Console.WriteLine($"Failed to post to Teams: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception posting to Teams: {ex.Message}");
            }
        }
    }
}
