using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using System.Net.Http;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook;

public class Function
{
    public async Task<string> FunctionHandler(object input, ILambdaContext context)
    {
        context.Logger.LogInformation($"FunctionHandler received: {input}");

        dynamic json = JsonConvert.DeserializeObject<dynamic>(input.ToString());
        dynamic payloadData = null;

        // Handle both possible input shapes
        try
        {
            if (json.issue != null)
            {
                payloadData = json;
            }
            else if (json.body != null)
            {
                payloadData = JsonConvert.DeserializeObject<dynamic>(json.body.ToString());
            }
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error parsing input: {ex.Message}");
            return "Error parsing input";
        }

        if (payloadData == null || payloadData.issue == null || payloadData.issue.html_url == null)
        {
            context.Logger.LogError("No issue.html_url found in payload");
            return "Invalid payload structure";
        }

        string issueUrl = (string)payloadData.issue.html_url;
        string payload = $"{{\"text\":\"Issue Created: {issueUrl}\"}}";

        var slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
        if (string.IsNullOrEmpty(slackUrl))
        {
            context.Logger.LogError("Missing SLACK_URL environment variable");
            return "SLACK_URL not set";
        }

        using var client = new HttpClient();
        var response = await client.PostAsync(slackUrl, new StringContent(payload, Encoding.UTF8, "application/json"));

        string result = await response.Content.ReadAsStringAsync();
        context.Logger.LogInformation($"Slack response: {result}");

        return result;
    }
}
