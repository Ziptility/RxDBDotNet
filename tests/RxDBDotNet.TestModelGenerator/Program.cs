// tests\RxDBDotNet.TestModelGenerator\Program.cs
using RxDBDotNet.Tests.Setup;

namespace RxDBDotNet.TestModelGenerator;

public sealed class Program
{
    public static async Task Main()
    {
        Console.WriteLine("Generating GraphQLTestModel...");

        var testContext = new TestScenarioBuilder().Build();

        try
        {
            await testContext.HttpClient.GenerateLiveDocsGraphQLClientAsync();

            Console.WriteLine("GraphQLTestModel generated successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating GraphQLTestModel: {ex.Message}");

            Environment.Exit(1);
        }
        finally
        {
            await testContext.DisposeAsync();
        }
    }
}
