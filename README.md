<div align="center">
  <img alt="ReDoc logo" src="https://raw.githubusercontent.com/catalyst-network/Community/master/media-pack/logo.png" width="400px" />

  ### Catalyst Framework - Full Stack Distributed Protocol Framework

[![Discord](https://img.shields.io/discord/629667101774446593?color=blueviolet&label=discord)](https://discord.gg/anTP7xm)
![GitHub followers](https://img.shields.io/github/followers/catalyst-network?style=social)
![GitHub stars](https://img.shields.io/github/stars/catalyst-network/community?style=social)
![Twitter Follow](https://img.shields.io/twitter/follow/catalystnetorg?style=social)
![Subreddit subscribers](https://img.shields.io/reddit/subreddit-subscribers/catalystnet?style=social)
</div>

<div align="center">
  
[![Build Status](https://dev.azure.com/catalyst-network/catalyst.core/_apis/build/status/Dev%20Test%20Suite?branchName=develop)](https://dev.azure.com/catalyst-network/catalyst.core/_build/latest?definitionId=3&branchName=develop)
[![Codacy Badge](https://api.codacy.com/project/badge/Grade/0940fa58afc24dbf96ad566f1fdc1390)](https://www.codacy.com?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=catalyst-network/Catalyst.Node&amp;utm_campaign=Badge_Grade)
[![All Contributors](https://img.shields.io/badge/all_contributors-7-orange.svg?style=flat-square)](#contributors)

</div>

<hr/>

Join us on our [Discord](https://discord.gg/anTP7xm) for any questions and discussions.

**Table of Contents**

- [Background](#background)
  - [What is the Catalyst Network?](#what-is-the-catalyst-network)
  - [What is Catalyst.Node?](#what-is-catalystnode)
  - [Features](#features)
  - [Documentation](#documentation)
- [Install](#install)
  - [Install the Rust Toolchain](#install-the-rust-toolchain)
    - [Install Rust via the Rustup tool:](#install-rust-via-the-rustup-tool)
- [Contributors](#contributors)
- [Contributing](#contributing)
- [License](#license)


## Background

### What is the Catalyst Network?

Catalyst Network is the distributed operating system that enterprise and developers can get straight on. No language barriers.  No architecture constraints. Scalable, fast and lean. The first DLT that is fit for purpose.

The decentralised protocol is designed from engineering first principles and built by an experienced team of software developers and financial service experts. With an enterprise focus it allows industries to leverage decentralisation into their operations.

### Features

- Protobuffs wire format ([see why](https://github.com/catalyst-network/protocol-protobuffs#why-protobuffs))
- RPC core protocol methods
- Distributed file storage (DFS) built upon IPFS
- Confidential and Public transactions
- Fast new and novel consensus Probabalistic BFT
- Flexible modula design

### Documentation

Our api docs can be found on our documentation site [https://catalyst-network.github.io/Catalyst.Framework/](https://catalyst-network.github.io/Catalyst.Framework)

## Install

To get started very quickly, you can do this:

```sh
# Clone the repo and the submodules
git clone git@github.com:catalyst-network/Catalyst.Node.git 
cd Catalyst.Node
git submodule update --init --force --recursive
cd src

# Then run the build
dotnet restore
dotnet build
```

More details on how to get going can be found in the [Quick Start Guide](https://github.com/catalyst-network/Catalyst.Node/wiki/Quick-Start-Guide).

### Install the Rust Toolchain

Catalyst.Core uses our native [Rust BulletProof library](https://github.com/catalyst-network/Cryptography.FFI.Rust).

To build the solution will require installing [Rust](https://www.rust-lang.org/). `msbuild prebuild tasks` will then compile the Bulletproof library when you try to build the project.


#### Install Rust via the Rustup tool:

```curl https://sh.rustup.rs -sSf | sh```

If ```rustc --version``` fails, restart your console to ensure changes to ```PATH``` have taken effect.

Refer to the Rust Bulletproof library [repository](https://github.com/catalyst-network/Cryptography.FFI.Rust) for docs. If you have issues with this part of the installation, please raise them there.

### Framework modules


| Core Libraries | Description                           | Nuget |
|---------------------|---------------------------------------|-------|
| [Abstractions](https://catalyst-network.github.io/Catalyst.Framework/api/abstractions/Catalyst.Abstractions.html)        | Framework Abstractions and interfaces |   ![Nuget](https://img.shields.io/nuget/v/Catalyst.Abstractions )     |
| [Core Lib](https://catalyst-network.github.io/Catalyst.Framework/api/Core.Lib/Catalyst.Core.Lib.html)            | Core Catalyst libraries               |  ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Lib )     |
| [Protocol SDK](https://catalyst-network.github.io/Catalyst.Framework/api/Protocol/Catalyst.Protocol.Account.html)        | Catalyst protocol c# sdk              |   ![Nuget](https://img.shields.io/nuget/v/Catalyst.Protocol )     |
| **Core Modules**                                    | **Description**                                                                 | **Nuget** |
| [Kvm](https://catalyst-network.github.io/Catalyst.Framework/api/Core.Modules/Kvm/Catalyst.Core.Modules.Kvm.html)                       | Finite state machine for smart contacts                                     |   ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.Kvm)    |
| [Mempool](https://catalyst-network.github.io/Catalyst.Framework/api/Core.Modules/Mempool/Catalyst.Core.Modules.Mempool.html)                   | Deterministic mempool for ordering transactions                             |    ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.Mempool )   |
| [Web3](https://catalyst-network.github.io/Catalyst.Framework/api/Core.Modules/Web3/Catalyst.Core.Modules.Web3.html)                      | Web3 gaateway                                                               |    ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.Web3)   |
| [Consensus](https://catalyst-network.github.io/Catalyst.Framework/api/Core.Modules/Consensus/Catalyst.Core.Modules.Consensus.html)                 | PBFT Consensus Mechanism                                                    |    ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.Consensus )   |
| [Dfs](https://catalyst-network.github.io/Catalyst.Framework/api/Core.Modules/Dfs/Catalyst.Core.Modules.Dfs.html)                       | Distributed File Storage                                                    |     ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.Dfs )  |
| [Keystore](https://catalyst-network.github.io/Catalyst.Framework/api/Core.Modules/Keystore/Catalyst.Core.Modules.Keystore.html)                  | Secure Keystore                                                             |    ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.Keystore  )   |
| [Hastings Discovery](https://catalyst-network.github.io/Catalyst.Framework/api/Core.Modules/P2P.Discovery.Hastings/Catalyst.Core.Modules.P2P.Discovery.Hastings.html)    | Unstructured overlay network with metropolis hasting random walk            |   ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.P2P.Discovery.Hastings)    |
| [Rpc Server](https://catalyst-network.github.io/Catalyst.Framework/api/Core.Modules/Rpc.Server/Catalyst.Core.Modules.Rpc.Server.html)                | Rpc Server pipeline with dotnetty                                           |    ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.Rpc.Server)   |
| [Rpc Client](https://catalyst-network.github.io/Catalyst.Framework/api/Core.Modules/Rpc.Client/Catalyst.Core.Modules.Rpc.Client.html)                | Rpc Client pipeline with dotnetty                                           |    ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.Rpc.Client   )   |
| [KeySigner](https://catalyst-network.github.io/Catalyst.Framework/api/Core.Modules/KeySigner/Catalyst.Core.Modules.KeySigner.html)                 | Catalyst KeySigner, sign transactions and message with identity and context |   ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.KeySigner  )    |
| [Ledger](https://catalyst-network.github.io/Catalyst.Framework/api/Core.Modules/Ledger/Catalyst.Core.Modules.Ledger.html)                    | Catalyst ledger state provider                                              |     ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.Ledger  )  |
| [BulletProofs Cryptography](https://catalyst-network.github.io/Catalyst.Framework/api/Core.Modules/Cryptography.BulletProofs/Catalyst.Core.Modules.Cryptography.BulletProofs.html) | Bullet proof native rust bindings                                           |    ![Nuget](https://img.shields.io/nuget/v/Catalyst.Core.Modules.Cryptography.BulletProofs)   |
| **Optional Modules** | **Description**              | **Nuget** |
| [CosmosDB](https://catalyst-network.github.io/Catalyst.Framework/api/Modules/Repository.CosmosDb/Catalyst.Modules.Repository.CosmosDb.html)         | Azure CosmosDb connector |    ![Nuget](https://img.shields.io/nuget/v/Catalyst.Modules.Repository.CosmosDb )   |
| [MongoDB](https://catalyst-network.github.io/Catalyst.Framework/api/Modules/Repository.MongoDb/Catalyst.Modules.Repository.MongoDb.html)          | MongoDb connector        |   ![Nuget](https://img.shields.io/nuget/v/Catalyst.Modules.Repository.MongoDb )    |
## Contributors

Thanks goes to these wonderful people ([emoji key](https://allcontributors.org/docs/en/emoji-key)):

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore -->
<table>
  <tr>
    <td align="center"><a href="https://github.com/nshcore"><img src="https://avatars2.githubusercontent.com/u/21375475?v=4" width="100px;" alt="nshCore"/><br /><sub><b>nshCore</b></sub></a><br /><a href="#infra-nshCore" title="Infrastructure (Hosting, Build-Tools, etc)">🚇</a> <a href="#ideas-nshCore" title="Ideas, Planning, & Feedback">🤔</a> <a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=nshCore" title="Tests">⚠️</a> <a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=nshCore" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/franssl"><img src="https://avatars0.githubusercontent.com/u/46971650?v=4" width="100px;" alt="franssl"/><br /><sub><b>franssl</b></sub></a><br /><a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=franssl" title="Tests">⚠️</a> <a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=franssl" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/monsieurleberre"><img src="https://avatars2.githubusercontent.com/u/4638821?v=4" width="100px;" alt="monsieurleberre"/><br /><sub><b>monsieurleberre</b></sub></a><br /><a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=monsieurleberre" title="Tests">⚠️</a> <a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=monsieurleberre" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/atlassanjay"><img src="https://avatars3.githubusercontent.com/u/49910176?v=4" width="100px;" alt="atlassanjay"/><br /><sub><b>atlassanjay</b></sub></a><br /><a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=atlassanjay" title="Tests">⚠️</a> <a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=atlassanjay" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/Millymanz"><img src="https://avatars3.githubusercontent.com/u/5070123?v=4" width="100px;" alt="Millymanz"/><br /><sub><b>Millymanz</b></sub></a><br /><a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=Millymanz" title="Tests">⚠️</a> <a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=Millymanz" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/richardschneider"><img src="https://avatars2.githubusercontent.com/u/631061?v=4" width="100px;" alt="Richard Schneider"/><br /><sub><b>Richard Schneider</b></sub></a><br /><a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=richardschneider" title="Tests">⚠️</a> <a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=richardschneider" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/Xela101"><img src="https://avatars0.githubusercontent.com/u/11431881?v=4" width="100px;" alt="Alex"/><br /><sub><b>Alex</b></sub></a><br /><a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=Xela101" title="Code">💻</a> <a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=Xela101" title="Tests">⚠️</a></td>
  </tr>
</table>

<!-- ALL-CONTRIBUTORS-LIST:END -->

## Contributing

Now that you've seen all of the contributors, why not contribute? We're always keen on getting more contributions and growing our community! Open a PR! Log an issue! :D

**Take a look at our organization-wide [Contributing Guide](https://github.com/catalyst-network/Community/blob/master/CONTRIBUTING.md).** You'll find most of your questions answered there.

As far as code goes, we would be happy to accept PRs! If you want to work on something, it'd be good to talk beforehand to make sure nobody else is working on it. You can reach us [on Discord](https://discord.gg/anTP7xm), or in the [issues section](https://github.com/catalyst-network/Catalyst.Node/issues).

Please note that we have a [Code of Conduct](CODE_OF_CONDUCT.md), and that all activity in the [@catalyst-network](https://github.com/catalyst-network) organization falls under it. Read it when you get the chance, as being part of this community means that you agree to abide by it. Thanks.

## License

[GPL](LICENSE) © 2019 Catalyst Network
