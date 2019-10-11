# Integration tests in Catalyst.Node

>As explained in [How is Autofac used in Catalyst.Node](autofac.md), the Catalyst node is using a set of configuration files to choose and configure its dependencies. When writing integration tests involving several layers of dependencies, it would be cumbersome to have to reconfigure a large part of the container for each new test.  
>This article will try to explain a couple of tips that should help to reduce the pain that comes with writing such tests.

## Using the file system

For integration tests to be able to run independently of each other whilst using the file system, they need to inherit from `Catalyst.Node.Common.UnitTests.TestUtils.FileSystemBaseTest`. This class creates a unique folder for each test run and might clean older folders upon getting disposed.  
As an example, this screenshot shows that a few folders have been created in the output folder of our test project. 
- The _Config_ folder contains configurations similar to the ones used by the node when running normally, which can be referenced to configure the container used in a given integration test.
- Other folders such as _Mempool_with_InMemoryRepo_... are created as the tests run. Each test run will result in a different timestamp suffix (this is to allow running the same test with different parameters, or the same `Theory` with different `DataAttribute` in Xunit terminology, in separate folder whilst keeping the ability to automatically clean old folders).

![Filesystem Int Tests](../images/filesystem-int-tests.png)

From here, we can now use the folders to set up configuration scenarios needed by the test, or to output logs and other diagnostics from the tests.

## An example from the code

Following is an integration test inheriting from `ConfigFileBasedTest`, which is the base class that can be used to write similar tests.

```csharp
public sealed class MessageCorrelationCacheIntegrationTests : ConfigFileBasedTest
{
    private readonly ILifetimeScope _scope;

    public MessageCorrelationCacheIntegrationTests(ITestOutputHelper output) : base(output)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
            .Build();

        ConfigureContainerBuilder(config, 
            writeLogsToTestOutput: true, 
            writeLogsToFile: true);

        var container = ContainerBuilder.Build();
        _scope = container.BeginLifetimeScope(CurrentTestName);
    }

    [Fact]
    [Trait(Traits.TestType, Traits.IntegrationTest)]
    public async Task RequestStore_should_not_keep_records_for_longer_than_ttl()
    {
        ...
    }
```

In the constructor of the test, 
1. We pass on the `ITestOutputHelper` to the parent class
2. Then we proceed with registering the only JSON configuration file needed by the test 
3. After that, we can call the ConfigureContainerBuilder on the parent class and mention which logging outputs we need. This method is virtual and can be overridden to add in extra registrations if needed.  
   _NB: Please don't check in code with logging output turned on, this is simply offered to help understand issues when writing tests_.
4. Finally, we build the container and instantiate a named scope for the test in which we will need dependency injection.

## Notes

- When trying to access the folder for your test on the file system, it is best to use the `protected readonly IFileSystem FileSystem` from `FileSystemBasedTest` and call the `GetCatalystHomeDir()` method. The method should return you the unique folder for this test run.

- You shouldn't need to worry about cleaning the test folders after each run: the `FileSystemBasedTest` class will try to clean folder from previous runs upon getting disposed.
