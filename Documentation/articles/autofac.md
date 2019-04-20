# How is Autofac used in Catalyst.Node

## Dependency injection (_DI_)

Dependency injection is a very common way to introduce inversion of control in the architecture of a piece of software. Although it might appear overly complicated at first, inversion of control tends to result in architectures that are more modular, extensible, and easier to test than their procedural counterparts... Well, at least that is what we read on the web:

<https://en.wikipedia.org/wiki/Inversion_of_control>  
<https://martinfowler.com/articles/injection.html>

Throughout the _Catalyst.Node_ solution, the popular Autofac for .Net framework is used to provide dependency injection.

## How is it done in Catalyst.Node

Catalyst.Node is composed of a bunch of mandatory components without which the node would basically be unable to function, as well as a bunch of _modules_, which are meant to be replaceable depending on your particular needs. 
For instance, it makes sense to think of a node running with a local file system versus an interplanetary one, with an in-memory mempool or with an SQL based one, but it is much harder to think of it with many different but compatible messaging protocols.  
These _modules_ are therefore treated as independent services and rely on their own separate configuration files.

### Main (mandatory) components

Most of the DI configuration for mandatory components can be found in the _`components.json`_ file. Here is how a basic configuration looks like:
 ```json
"defaultAssembly": "Catalyst.Node.Core",
"components": [
{
    "type": "Catalyst.Node.Core.CatalystNode",
    "services": [
    {
        "type": "Catalyst.Node.Common.Interfaces.ICatalystNode, Catalyst.Node.Common"
    }]
},
```
> `components -> type` : refers to the name of the type that should be used to satisfy the implementation of the various `services`.  
> `services` : this represents the list of interfaces that should be resolved using the concrete type defined above.  
> `defaultAssembly` : can be used to avoid having to explicitly specify the assembly in which a type should be found. in the example above, the type _Catalyst.Node.Core.CatalystNode_ should be found in the _Catalyst.Node.Core_ assembly. However _Catalyst.Node.Common.Interfaces.ICatalystNode_ should be found in _Catalyst.Node.Common_.  

A lot more information about Autofac configuration can be found [here](https://autofaccn.readthedocs.io/en/latest/configuration/xml.html#components).

It is in that same _`components.json`_ file that we can find links to the individual module configuration.

```json
"modules": [
   {
     "type": "Catalyst.Node.Core.Modules.JsonConfiguredModule",
     "parameters": {
       "configFilePath": "Config/Modules/dfs.json"
     }
   },
```
> `module` : groups a set of dependencies that only make sense in the context of a higher level functionality. For instance, a _Dfs_ (Distributed File System) module might need an _INetworkConnector_ that is not needed by, and might not even make sense to, a _Cryptography_ module only supposed to group a bunch of mathematical functions used to encrypt messages.  
> `type` : is a type inheriting from _Autofac.Module_ which defines how the module registers its dependencies. In our case, a JsonConfiguredModule has been implemented to allow a given module to read its own configuration, from a separate JSON file found at _configFilePath_

More information about Autofac modules configuration can be found [here](https://autofaccn.readthedocs.io/en/latest/configuration/xml.html#modules).

### Modules configuration

As explained above, modules are meant to represent isolate and interchangeable sets of related functionalities. In Catalyst.Node, they rely on a given configuration file, which is read through the _JsonConfiguredModule_ class. This is not mandatory and different modules could be implemented and configured differently. However, we decided to use this mechanism to allow different users of the node to swap the implementation of a given module simply by changing its configuration.

The source code contains an example of such a configuration change in the class _MempoolIntegrationTests_ where a first mempool is configured to use an in-memory storage mechanism, while a second one is configured to use an XML file based store. 
```csharp
[Fact]
[Trait(Traits.TestType, Traits.IntegrationTest)]
public async Task Mempool_with_InMemoryRepo_can_save_and_retrieve()
{
    var fi = new FileInfo(Path.Combine(Constants.ConfigSubFolder, Constants.ModulesSubFolder,
        "mempool.inmemory.json"));
    await Mempool_can_save_and_retrieve(fi);
}

[Fact]
[Trait(Traits.TestType, Traits.IntegrationTest)]
public async Task Mempool_with_XmlRepo_can_save_and_retrieve()
{
    var mempoolConfigFile = "mempool.xml.json";
    var resultFile = await PointXmlConfigToLocalTestFolder(mempoolConfigFile);
    await Mempool_can_save_and_retrieve(new FileInfo(resultFile));
}
```

## Autofac documentation
This article is only meant to explain briefly how Autofac is used in the Catalyst.Node solution, but it is advised to read the [Autofac documentation](https://autofaccn.readthedocs.io/en/latest/index.html) which contains more in-depth information and helpful examples.
