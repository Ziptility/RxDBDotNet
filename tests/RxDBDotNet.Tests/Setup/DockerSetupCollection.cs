namespace RxDBDotNet.Tests.Setup;

[CollectionDefinition("Docker collection")]
public class DockerSetupCollection : ICollectionFixture<DockerSetupFixture>
{
    // This class has no code, and is never created.
    // Its purpose is to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
