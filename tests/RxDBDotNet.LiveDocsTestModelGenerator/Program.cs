using RxDBDotNet.Tests.Setup;

namespace RxDBDotNet.LiveDocsTestModelGenerator;

public sealed class Program
{
    public static async Task Main()
    {
        Console.WriteLine("Generating LiveDocsGraphQLClient...");

        var factory = WebApplicationFactorySetup.CreateWebApplicationFactory();

        using var client = factory.CreateHttpClient();

        try
        {
            await client.GenerateLiveDocsGraphQLClientAsync();

            Console.WriteLine("LiveDocsGraphQLClient generated successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating LiveDocsGraphQLClient: {ex.Message}");

            Environment.Exit(1);
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }
}
