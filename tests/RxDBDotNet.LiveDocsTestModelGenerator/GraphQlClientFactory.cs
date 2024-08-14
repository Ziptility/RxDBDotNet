using System.Text;
using GraphQlClientGenerator;
using Newtonsoft.Json;

namespace RxDBDotNet.LiveDocsTestModelGenerator;

public static class GraphQlClientFactory
{
    /// <summary>
    ///     Generates the GraphQL client based on the latest schema from the live docs.
    ///     This generated class will be used the next time the code compiles and the tests are run.
    /// </summary>
    /// <param name="httpClient">The test HttpClient</param>
    public static async Task GenerateLiveDocsGraphQLClientAsync(this HttpClient httpClient)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(httpClient);

            using var requestContent = new StringContent(JsonConvert.SerializeObject(new
            {
                query = IntrospectionQuery.Text,
            }), Encoding.UTF8, "application/json");

            var schemaResponse = await httpClient.PostAsync("/graphql", requestContent);
            var schemaString = await schemaResponse.Content.ReadAsStringAsync();
            var schema = GraphQlGenerator.DeserializeGraphQlSchema(schemaString);

            var config = new GraphQlGeneratorConfiguration
            {
                GeneratePartialClasses = true,
                EnumValueNaming = EnumValueNamingOption.CSharp,
                ScalarFieldTypeMappingProvider = new ScalarFieldTypeMappingProvider(),
                ClassSuffix = "Gql",
                CSharpVersion = CSharpVersion.NewestWithNullableReferences,
            };

            var generator = new GraphQlGenerator(config);
            var csharpCode = generator.GenerateFullClientCSharpFile(schema, "RxDBDotNet.Tests.Model");

            // Split the generated code into lines
            var lines = csharpCode.Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();

            // Insert the pragma warning disable line
            // since the GraphQlClientGenerator library does not correctly handle nullable reference types
            lines.Insert(1, "#pragma warning disable 8618");

            // Join the lines back into a single string
            var modifiedCsharpCode = string.Join(Environment.NewLine, lines);

            var currentDirectory = Directory.GetCurrentDirectory();

            Console.WriteLine($"The current directory is '{currentDirectory}'");

            await File.WriteAllTextAsync("./Model/LiveDocsGraphQLClient.cs", modifiedCsharpCode);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
