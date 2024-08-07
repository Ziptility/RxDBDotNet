namespace LiveDocs.GraphQLApi.Infrastructure;

public static class ConfigKeys
{
    /// <summary>
    ///     The database connection string.
    /// </summary>
    public const string DbConnectionString = "SQL_SERVER";

    /// <summary>
    ///     Whether the app is running in the context of a .NET Aspire host.
    /// </summary>
    public const string IsAspireEnvironment = "IS_ASPIRE_ENVIRONMENT";
}
