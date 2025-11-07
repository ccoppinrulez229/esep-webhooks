using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.IO;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook;

public class Function
{
    public string FunctionHandler(object input, ILambdaContext context)
    {
        context.Logger.LogInformation($"FunctionHandler received: {input}");

        var json = JObject.Parse(input.ToString());
        context.Logger.LogInformation($"JSON: {json}");

        var issueUrl = json["body"]?["issue"]?["url"]?.ToString() ?? "No URL found";

        string payload = JsonConvert.SerializeObject(new { text = $"Issue Created: {issueUrl}" });

        using var client = new HttpClient();
        var webRequest = new HttpRequestMessage(HttpMethod.Post, Environment.GetEnvironmentVariable("SLACK_URL"))
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        var response = client.Send(webRequest);
        using var reader = new StreamReader(response.Content.ReadAsStream());

        return reader.ReadToEnd();
    }
}
