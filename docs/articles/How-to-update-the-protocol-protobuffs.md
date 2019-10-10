# This page is out of date, please refer to this guide for up to date information

***
[Publish a new Catalyst.Protocol nuget package](https://github.com/catalyst-network/protobuffs-protocol-sdk-csharp/wiki/Publish-a-new-Catalyst.Protocol-nuget-package)

## How to update the protocol-protobuffs
The Catalyst.Node uses protobuffs for its wire message format. You can read why [here](https://github.com/catalyst-network/protocol-protobuffs#why-protobuffs)

The [protocol-protobuffs](https://github.com/catalyst-network/protocol-protobuffs) project is included in the [Catalyst.Node](https://github.com/catalyst-network/Catalyst.Node) project via a git sub-module, to install them you can read the instructions in the [quick start guide](https://github.com/catalyst-network/Catalyst.Node/wiki/Quick-Start-Guide#3-clone-git-sub-modules).

The protobuffs are built by the [Catalyst.Common project solution](https://github.com/catalyst-network/Catalyst.Node/blob/develop/src/Catalyst.Common/Catalyst.Common.csproj) on every build. You will see the raw files in 

>Catalyst.Common/Protocol/Protobuffs 

with the compiled C# files in 

>Catalyst.Common/Protocol

## How to work with them
So this is just one method of working with them files. Feel free to work any other way if you find it easier

Assuming you Catalyst.Node code lives in

> ~/Catalyst.Node

git clone the protocol-protobuffs to 

> ~/protocol-protobuffs

Make you changes in a new branch within that protobuffs repository

> cd ~/protocol-protobuffs
> git branch my-cool-new-feature
> do some work
> git add --all; git commit --all -m"some meaningful message"; git push origin my-cool-new-feature
To receive these updates in your Catalyst.Node code base, you need to tell the sub-module to track the branch in protocol-protobuffs "my-cool-new-feature"

> cd ~/Catalyst.Node/submodules/protocol-protobuffs
> git checkout origin my-cool-new-feature

you can then rebuild the solution to see your changes. Every time you change your protobuff in the separate project you will need to push it to you "my-new-cool-feature" branch and pull it down in Catalyst.Node. But it's not so cumbersome if you use a GUI. [Git Kraken](https://www.gitkraken.com/) is my choice here as it has native support for sub-modules as well as more advanced git features.

## How to merge

How to prepare your pull request is important. If you make a PR first for Catalyst.Node with out merging the protocol-protobuffs what will happen is Catalyst.Node develop will start tracking "my-cool-new-feature" in the protocol-protobuffs project. This is not desirable as what can happen is that branch can get deleted once fully merged in the protobuffs project then develop will fail to build

**ALWAYS MERGE PROTOCOL-PROTOBUFFS FIRST**
 
Always push and make a PR on protocol-protobuffs first. Once these changes make their way into develop, you now need to tell you Catalyst.Node branch submodule for protobuffs to follow develop again. This way when you do a PR in Catalyst.Node and it merges into develop, it will still track develop

> cd ~/Catalyst.Node/submodules/protocol-protobuffs
> git checkout origin develop
> git pull origin develop

now commit your work in Catalyst.Node and push up.

### Can't I make a PR on Catalyst.Node and protocol-protobuffs at the same time?
Yes you could, but you shouldn't. Reviewers should never approve a PR going to develop that has changes done to the protocol without seeing them on develop. For two reasons.

1. It's inefficient as you will have to update your PR and take time on the build pipeline to rebuild your PR just for a sub-module update
2. The [Protocol Team](https://github.com/orgs/catalyst-network/teams/protocol-team) need to approve the changes as well. Since this is the protocol and it is a common message format that can effect many projects. The protocol team must be consulted on all the changes and give approval from all stakeholders