<div align="center">
  <img alt="ReDoc logo" src="https://raw.githubusercontent.com/catalyst-network/Community/master/media-pack/logo.png" width="400px" />

  ### Catalyst - Full Stack Distributed Protocol Framework
 
[![Website](https://catalystnet.org/)
[![Subreddit subscribers](https://img.shields.io/reddit/subreddit-subscribers/catalystnet?style=social)](https://reddit.com/r/catalystnet)
</div>

<div align="center">
  
[![Build Status](https://dev.azure.com/catalyst-network/Catalyst/_apis/build/status/pr-tests-master?branchName=master)](https://dev.azure.com/catalyst-network/Catalyst/_build/latest?definitionId=22&branchName=master)
[![Codacy Badge](https://api.codacy.com/project/badge/Grade/08cb016d7489471eadd0192ce4d7b26e)](https://www.codacy.com/manual/catalyst-network/Catalyst?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=catalyst-network/Catalyst&amp;utm_campaign=Badge_Grade)
[![All Contributors](https://img.shields.io/badge/all_contributors-11-orange.svg?style=flat-square)](#contributors)

</div>

<hr/>



**Important Release Notes 31/03/2020**

***Known UDP messaging issue***
For the initial release of the Catalyst Network UDP is used as the peer to peer messaging protocol. This causes fragmentation of packages and thereby transactions over 1280Bytes are prone to packet loss as discussed in [Issue #909](https://github.com/catalyst-network/Catalyst/issues/909). Due to the DFS utilising a seperate TCP messaging system this does not affect files stored in the DFS including ledger state updates. However it will affect both simple and smart contract transactions. 

***MacOS node running issue***
Currently there is an issue with running RocksDB on MacOS meaning that a node can not unfortunatly be run on MacOS. A fix is currently in progress and will be availiable iminently. 


**Table of Contents**

- [Background](#background)
  - [What is the Catalyst Network?](#what-is-the-catalyst-network)
  - [What is Catalyst.Node?](#what-is-catalystnode)
  - [Features](#features)
  - [Documentation](#documentation)
- [Set Up a Node](#install)
  - [Install the Rust Toolchain](#install-the-rust-toolchain)
    - [Install Rust via the Rustup tool:](#install-rust-via-the-rustup-tool)
- [Contributors](#contributors)
- [Contributing](#contributing)
- [License](#license)


## What is the Catalyst Network?

The Catalyst Network is a full-stack distributed network built to fulfill the real-world potential of Distributed Ledger Technology, enabling the next generation of distributed computing applications and
business models.

Catalyst was designed by an experienced team of engineers and researchers who were presented with a difficult challenge: build a large decentralized network capable of storing all types of data ranging from structured tabular records through to large Binary Large Objects at low cost to users, as well as dApps written in any language. Broadly, this meant solving the blockchain trilemma to maintain decentralization and support a high transaction throughput in a continuously growing network without compromising on security.

#### Features

- Protobuffs wire format ([see why](https://github.com/catalyst-network/protocol-protobuffs#why-protobuffs))
- RPC core protocol methods
- Distributed file storage built upon IPFS
- Confidential and Public transactions
- Fast new and novel consensus Probabilistic BFT
- Flexible modular design

## Documentation

Our api docs can be found on our documentation site [https://catalyst-network.github.io/Catalyst/api](https://catalyst-network.github.io/Catalyst/api)

Furtermore, our Technical White Paper is availiable [here](https://github.com/catalyst-network/whitepaper). This document explains the implementation of the Catalyst network. 

## Quick Start Guide for Node

This is a quick start guide for running a node on the Catalyst test network. 

#### 1. Install .Net

Catalyst.Node works with .Net Core v3.0. You'll need to have the .Net SDK installed. 

If you don't have .Net Core installed you can follow the instructions for your platform bellow.

- [Windows](https://dotnet.microsoft.com/download?initial-os=windows)
- [Linux](https://dotnet.microsoft.com/download?initial-os=linux)
- [macOS](https://dotnet.microsoft.com/download?initial-os=macos)

#### 2. Install the Rust Toolchain

Catalyst.Core uses our native [Rust BulletProof library](https://github.com/catalyst-network/Cryptography.FFI.Rust). In order to be able to build the solution, you will need to ensure that the [Rust](https://www.rust-lang.org/) toolchain is correctly installed on you machine.

##### Unix environments

Download and install `msbuild prebuild tasks` from [Rust](https://www.rust-lang.org/). MsBuild will then compile the Bulletproof library when you try to build the project.

Then, make sure you install Rust using the rustup tool:

`curl https://sh.rustup.rs -sSf | sh`

If `rustc --version` fails, restart your console to ensure changes to `PATH` have taken effect.

Refer to the Rust cryptography library [repository](https://github.com/catalyst-network/Catalyst-rs) for docs. If you have issues with this part of the installation, please raise them there.

##### Windows environments
If you have not done so before, download and install the Microsoft Visual C++ Build Tools 2019 from https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2019. Alternatively, if you are using Visual Studio, you should be able to modify your existing installation to add this feature.

Go to https://www.rust-lang.org/tools/install, then download and execute `rustup-init.exe`

#### 3. Install Snappy for rocksdb (Unix systems only)
Snappy must be installed for Linux and MacOS opperating sytems 

For linux using:
`sudo apt-get install libsnappy-dev`

For MacOS install brew following: 
`https://brew.sh/`

Then: 

`brew install snappy`


#### 4. Create a self signed certificate (Linux only)

If you're on Linux, you need to create a Self Signed Certificate the instructions for which are found here:

[Create a Self Signed Certificate](https://github.com/catalyst-network/Catalyst.Node/wiki/Create-a-Self-Signed-Certificate)

#### 5. Install MongoDB
Instructions to install MongoDB can be found [here](https://docs.mongodb.com/manual/administration/install-community/) for each operating system. 

#### 6. Clone the repository

To clone the repository it is assumed you have Git installed.
If you do not, then follow the [Git install instructions](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git) for Linux/Windows/macOS.

> ##### Windows environment prerquisite -  Enable long paths
> make sure that long paths are allowed in both windows and git
> - for windows 
>     follow the instructions at https://docs.microsoft.com/en-us/windows/win32/fileio/naming-a-file#enable-long-paths-in-windows-10-version-1607-and-later
> - for git : 
>     start a git terminal as administrator
>     run the following command : `git config --system core.longpaths true`

To clone the Catalyst repository use the command:

`git clone https://github.com/catalyst-network/Catalyst.git`


Then navigate into the repository:

`cd Catalyst`

Install the dependencies using the command: 

`git submodule update --init --recursive --force`



#### 7. Build the solution

Navigate to the `src` folder:

`cd src`



In the `src` folder build the solution:

`dotnet build Catalyst.sln`

#### 8. Run the node 

To run the node change to: 

`cd Catalyst.Node.POA.CE`

Then use the command 

`dotnet run`



### Configuring the node

Once the above steps have been completed the node must be manually configured following:

[How to configure a Catalyst POA node](https://github.com/catalyst-network/Catalyst/wiki/Configuring-a-Catalyst-POA-Node)


## Modules

| Core Libraries | Description                           | Nuget |
|---------------------|---------------------------------------|-------|
| [Abstractions](https://catalyst-network.github.io/Catalyst/api/abstractions/Catalyst.Abstractions.html)        |Abstractions and interfaces |   ![Nuget](https://img.shields.io/nuget/v/Catalyst.Abstractions )     |
| [Core Lib](https://catalyst-network.github.io/Catalyst/api/Core.Lib/Catalyst.Core.Lib.html)            | Core Catalyst libraries               |  ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Lib )     |
| [Protocol SDK](https://catalyst-network.github.io/Catalyst/api/Protocol/Catalyst.Protocol.Account.html)        | Catalyst protocol c# sdk              |   ![Nuget](https://img.shields.io/nuget/v/Catalyst.Protocol )     |
| **Core Modules**                                    | **Description**                                                                 | **Nuget** |
| [Kvm](https://catalyst-network.github.io/Catalyst/api/Core.Modules/Kvm/Catalyst.Core.Modules.Kvm.html)                       | Finite state machine for smart contacts                                     |   ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.Kvm)    |
| [Mempool](https://catalyst-network.github.io/Catalyst/api/Core.Modules/Mempool/Catalyst.Core.Modules.Mempool.html)                   | Deterministic mempool for ordering transactions                             |    ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.Mempool )   |
| [Web3](https://catalyst-network.github.io/Catalyst/api/Core.Modules/Web3/Catalyst.Core.Modules.Web3.html)                      | Web3 gaateway                                                               |    ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.Web3)   |
| [Consensus](https://catalyst-network.github.io/Catalyst/api/Core.Modules/Consensus/Catalyst.Core.Modules.Consensus.html)                 | PBFT Consensus Mechanism                                                    |    ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.Consensus )   |
| [Dfs](https://catalyst-network.github.io/Catalyst/api/Core.Modules/Dfs/Catalyst.Core.Modules.Dfs.html)                       | Distributed File Storage                                                    |     ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.Dfs )  |
| [Keystore](https://catalyst-network.github.io/Catalyst/api/Core.Modules/Keystore/Catalyst.Core.Modules.Keystore.html)                  | Secure Keystore                                                             |    ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.Keystore  )   |
| [Hastings Discovery](https://catalyst-network.github.io/Catalyst/api/Core.Modules/P2P.Discovery.Hastings/Catalyst.Core.Modules.P2P.Discovery.Hastings.html)    | Unstructured overlay network with metropolis hasting random walk            |   ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.P2P.Discovery.Hastings)    |
| [Rpc Server](https://catalyst-network.github.io/Catalyst/api/Core.Modules/Rpc.Server/Catalyst.Core.Modules.Rpc.Server.html)                | Rpc Server pipeline with dotnetty                                           |    ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.Rpc.Server)   |
| [Rpc Client](https://catalyst-network.github.io/Catalyst/api/Core.Modules/Rpc.Client/Catalyst.Core.Modules.Rpc.Client.html)                | Rpc Client pipeline with dotnetty                                           |    ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.Rpc.Client   )   |
| [KeySigner](https://catalyst-network.github.io/Catalyst/api/Core.Modules/KeySigner/Catalyst.Core.Modules.KeySigner.html)                 | Catalyst KeySigner, sign transactions and message with identity and context |   ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.KeySigner  )    |
| [Ledger](https://catalyst-network.github.io/Catalyst/api/Core.Modules/Ledger/Catalyst.Core.Modules.Ledger.html)                    | Catalyst ledger state provider                                              |     ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.Ledger  )  |
| [BulletProofs Cryptography](https://catalyst-network.github.io/Catalyst/api/Core.Modules/Cryptography.BulletProofs/Catalyst.Core.Modules.Cryptography.BulletProofs.html) | Bullet proof native rust bindings                                           |    ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.Cryptography.BulletProofs)   |
| **Optional Modules** | **Description**              | **Nuget** |
| [CosmosDB](https://catalyst-network.github.io/Catalyst/api/Modules/Repository.CosmosDb/Catalyst.Modules.Repository.CosmosDb.html)         | Azure CosmosDb connector |    ![Nuget](https://img.shields.io/nuget/v/Catalyst.Modules.Repository.CosmosDb )   |
| [MongoDB](https://catalyst-network.github.io/Catalyst/api/Modules/Repository.MongoDb/Catalyst.Modules.Repository.MongoDb.html)          | MongoDb connector        |   ![Nuget](https://img.shields.io/nuget/v/Catalyst.Modules.Repository.MongoDb )    |
## Contributors

Thanks goes to these wonderful people ([emoji key](https://allcontributors.org/docs/en/emoji-key)):

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore -->
<table>
  <tr>
    <td align="center"><a href="https://hub.docker.com/u/jkirkby91"><img src="https://avatars2.githubusercontent.com/u/21375475?v=4" width="100px;" alt="nshCore"/><br /><sub><b>nshCore</b></sub></a><br /><a href="#infra-nshCore" title="Infrastructure (Hosting, Build-Tools, etc)">🚇</a> <a href="#ideas-nshCore" title="Ideas, Planning, & Feedback">🤔</a> <a href="https://github.com/catalyst-network/Catalyst/commits?author=nshCore" title="Tests">⚠️</a> <a href="https://github.com/catalyst-network/Catalyst/commits?author=nshCore" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/franssl"><img src="https://avatars0.githubusercontent.com/u/46971650?v=4" width="100px;" alt="franssl"/><br /><sub><b>franssl</b></sub></a><br /><a href="https://github.com/catalyst-network/Catalyst/commits?author=franssl" title="Tests">⚠️</a> <a href="https://github.com/catalyst-network/Catalyst/commits?author=franssl" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/monsieurleberre"><img src="https://avatars2.githubusercontent.com/u/4638821?v=4" width="100px;" alt="monsieurleberre"/><br /><sub><b>monsieurleberre</b></sub></a><br /><a href="https://github.com/catalyst-network/Catalyst/commits?author=monsieurleberre" title="Tests">⚠️</a> <a href="https://github.com/catalyst-network/Catalyst/commits?author=monsieurleberre" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/atlassanjay"><img src="https://avatars3.githubusercontent.com/u/49910176?v=4" width="100px;" alt="atlassanjay"/><br /><sub><b>atlassanjay</b></sub></a><br /><a href="https://github.com/catalyst-network/Catalyst/commits?author=atlassanjay" title="Tests">⚠️</a> <a href="https://github.com/catalyst-network/Catalyst/commits?author=atlassanjay" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/Millymanz"><img src="https://avatars3.githubusercontent.com/u/5070123?v=4" width="100px;" alt="Millymanz"/><br /><sub><b>Millymanz</b></sub></a><br /><a href="https://github.com/catalyst-network/Catalyst/commits?author=Millymanz" title="Tests">⚠️</a> <a href="https://github.com/catalyst-network/Catalyst/commits?author=Millymanz" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/richardschneider"><img src="https://avatars2.githubusercontent.com/u/631061?v=4" width="100px;" alt="Richard Schneider"/><br /><sub><b>Richard Schneider</b></sub></a><br /><a href="https://github.com/catalyst-network/Catalyst/commits?author=richardschneider" title="Tests">⚠️</a> <a href="https://github.com/catalyst-network/Catalyst/commits?author=richardschneider" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/Xela101"><img src="https://avatars0.githubusercontent.com/u/11431881?v=4" width="100px;" alt="Alex"/><br /><sub><b>Alex</b></sub></a><br /><a href="https://github.com/catalyst-network/Catalyst/commits?author=Xela101" title="Code">💻</a> <a href="https://github.com/catalyst-network/Catalyst/commits?author=Xela101" title="Tests">⚠️</a></td>
  </tr>
  <tr>
    <td align="center"><a href="https://burntfen.com"><img src="https://avatars3.githubusercontent.com/u/910753?v=4" width="100px;" alt="Richard Littauer"/><br /><sub><b>Richard Littauer</b></sub></a><br /><a href="https://github.com/catalyst-network/Catalyst/commits?author=RichardLitt" title="Documentation">📖</a></td>
    <td align="center"><a href="https://github.com/sudhirtibrewal"><img src="https://avatars1.githubusercontent.com/u/12657704?v=4" width="100px;" alt="sudhirtibrewal"/><br /><sub><b>Sudhir Tibrewal</b></sub></a><br /><a href="https://github.com/sudhirtibrewal" title="Code">💻</a></td>
    <td align="center"><a href="http://nethermind.io"><img src="https://avatars1.githubusercontent.com/u/498913?v=4" width="100px;" alt="Tomasz Kajetan Stańczak"/><br /><sub><b>Tomasz Kajetan Stańczak</b></sub></a><br /><a href="https://github.com/catalyst-network/Catalyst/commits?author=tkstanczak" title="Code">💻</a> <a href="#ideas-tkstanczak" title="Ideas, Planning, & Feedback">🤔</a></td>
    <td align="center"><a href="http://blog.scooletz.com/"><img src="https://avatars1.githubusercontent.com/u/519707?v=4" width="100px;" alt="Szymon Kulec"/><br /><sub><b>Szymon Kulec</b></sub></a><br /><a href="https://github.com/catalyst-network/Catalyst/commits?author=Scooletz" title="Code">💻</a></td>
  </tr>
</table>

<!-- ALL-CONTRIBUTORS-LIST:END -->

## Contributing

Now that you've seen all of the contributors, why not contribute? We're always keen on getting more contributions and growing our community! Open a PR! Log an issue! :D

**Take a look at our organization-wide [Contributing Guide](https://github.com/catalyst-network/Community/blob/master/CONTRIBUTING.md).** You'll find most of your questions answered there.

As far as code goes, we would be happy to accept PRs! If you want to work on something, it'd be good to talk beforehand to make sure nobody else is working on it. You can reach us in the [issues section](https://github.com/catalyst-network/Catalyst.Node/issues).

Please note that we have a [Code of Conduct](CODE_OF_CONDUCT.md), and that all activity in the [@catalyst-network](https://github.com/catalyst-network) organization falls under it. Read it when you get the chance, as being part of this community means that you agree to abide by it. Thanks.

## License

[GPL](LICENSE) © 2019 Catalyst Network
