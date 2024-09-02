namespace RxDBDotNet.Tests.Utils;

[CollectionDefinition("DockerSetup")]
public class DockerSetupCollection : ICollectionFixture<DockerSetupUtil>
{
    // This class has no code, and is never created.
    // Its purpose is to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces so that the docker containers are initialized
    // once per test run.
}
