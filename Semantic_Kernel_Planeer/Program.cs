// See https://aka.ms/new-console-template for more information
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Planning.Handlebars;
public class Program
{
    private static Kernel _kernel;
    private static SecretClient keyVaultClient;
    public async static Task Main(string[] args)
    {
        IConfiguration config = new ConfigurationBuilder()
                      .AddUserSecrets<Program>()
                      .Build();

        string? appTenant = config["appTenant"];
        string? appId = config["appId"] ?? null;
        string? appPassword = config["appPassword"] ?? null;
        string? keyVaultName = config["KeyVault"] ?? null;

        var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
        ClientSecretCredential credential = new ClientSecretCredential(appTenant, appId, appPassword);
        keyVaultClient = new SecretClient(keyVaultUri, credential);
        string? apiKey = keyVaultClient.GetSecret("OpenAIapiKey").Value.Value;
        string? orgId = keyVaultClient.GetSecret("OpenAIorgId").Value.Value;

        var _builder = Kernel.CreateBuilder()
           .AddOpenAIChatCompletion("gpt-3.5-turbo", apiKey, orgId, serviceId: "gpt35")
            .AddOpenAIChatCompletion("gpt-4", apiKey, orgId, serviceId: "gpt4");

        _kernel = _builder.Build();
        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(),"plugins", "jokes");
        _kernel.ImportPluginFromPromptDirectory(pluginsDirectory);
#pragma warning disable SKEXP0060
        var plannerOptions = new HandlebarsPlannerOptions()
        {
            ExecutionSettings = new OpenAIPromptExecutionSettings()
            {
                Temperature = 0.0,
                TopP = 0.1,
                MaxTokens = 4000
            },
            AllowLoops = true
        };
        var planner = new HandlebarsPlanner(plannerOptions);
        var ask = "Tell four knock-knock jokes: two about dogs, one about cats and one about ducks";
        var plan = await planner.CreatePlanAsync(_kernel, ask);
        var result = await plan.InvokeAsync(_kernel);
        Console.Write($"Results: {result}");

        Console.ReadLine();
    }
}
