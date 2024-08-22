using RxDBDotNet.Tests.Setup;

namespace RxDBDotNet.TestModelGenerator;

public sealed class Program
{
    public static async Task Main()
    {
        Console.WriteLine("Generating GraphQLTestModel...");

        var factory = WebApplicationFactorySetupUtil.Setup();

        using var client = factory.CreateHttpClient();

        try
        {
            await client.GenerateLiveDocsGraphQLClientAsync();

            Console.WriteLine("GraphQLTestModel generated successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating GraphQLTestModel: {ex.Message}");

            Environment.Exit(1);
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }
}
