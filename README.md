# <p style="text-align: center;"> Catalyst Network </p>

|Windows Build   |OSX Build   |Linux Build   | Code Quality | Slack Chat | Contributors
|:-:	|:-:	|:-:	|:-:	|:-: |:-: |
|[![Build Status](https://dev.azure.com/AtlasCityIO/catalyst-network/_apis/build/status/catalyst-network-windows-build-develop?branchName=develop)](https://dev.azure.com/AtlasCityIO/catalyst-network/_build/latest?definitionId=3&branchName=develop)   	|[![Build Status](https://dev.azure.com/AtlasCityIO/catalyst-network/_apis/build/status/catalyst-network-osx-build-develop?branchName=develop)](https://dev.azure.com/AtlasCityIO/catalyst-network/_build/latest?definitionId=2&branchName=develop)   	|[![Build Status](https://dev.azure.com/AtlasCityIO/catalyst-network/_apis/build/status/catalyst-network-linux-build-develop?branchName=develop)](https://dev.azure.com/AtlasCityIO/catalyst-network/_build/latest?definitionId=1&branchName=develop)   	|[![Codacy Badge](https://api.codacy.com/project/badge/Grade/0940fa58afc24dbf96ad566f1fdc1390)](https://www.codacy.com?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=catalyst-network/Catalyst.Node&amp;utm_campaign=Badge_Grade)	| [<img src="https://img.shields.io/badge/slack-@catalystnet-purple.svg?logo=slack">](https://catalystnet.slack.com/messages/CGNJ845QV) | [![All Contributors](https://img.shields.io/badge/all_contributors-7-orange.svg?style=flat-square)](#contributors) |

<hr/>

## What is the Catalyst Network ?

Catalyst Network is the distributed operating system that enterprise and developers can get straight on. No language barriers.  No architecture constraints. Scalable, fast and lean. The first DLT that is fit for purpose.

The decentralised protocol is designed from engineering first principles and built by an experienced team of software developers and financial service experts. With an enterprise focus it allows industries to leverage decentralisation into their operations.

## What is Catalyst.Node ?

Catalyst.Node is the .Net implementation of the Catalyst Network. The spcification of our protocol is outlined in our technical while paper a more indepth implementation orientated document can be found in the [Protocol Blueprint](https://github.com/catalyst-network/protocol-blueprint/)

Catalyst.Node is developed on dotnet core 2.2 therefore supports Linux, macOS and Windows operating systems.

## Questions & Discussions

Join us on our [Slack Channel](https://catalystnet.slack.com/messages/CGNJ845QV) for any questions and discussions.

## Documentation

Is coming (don't you hate it when they say that). For now you can read through the project [wiki](https://github.com/catalyst-network/Catalyst.Node/wiki) or the [Protocol Blueprint](https://github.com/catalyst-network/protocol-blueprint/)

## Features

- Protobuffs wire format ([see why](https://github.com/catalyst-network/protocol-protobuffs#why-protobuffs))
- RPC core protocol methods
- Distributed file storage (DFS) built upon IPFS
- Confidential and Public transactions
- Fast new and novel consensus Probabalistic BFT
- Flexible modula design

## Real Quick Start Guide

`git clone git@github.com:catalyst-network/Catalyst.Node.git `

`cd Catalyst.Node`

`git submodule init`

`git submodule update`

`cd src`

`dotnet restore`

`dotnet build`

If that was really too quick try reading the [Quick Start Guide](https://github.com/catalyst-network/Catalyst.Node/wiki/Quick-Start-Guide)

## Contributors

Thanks goes to these wonderful people ([emoji key](https://allcontributors.org/docs/en/emoji-key)):

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore -->
<table>
  <tr>
    <td align="center"><a href="https://hub.docker.com/u/jkirkby91"><img src="https://avatars2.githubusercontent.com/u/21375475?v=4" width="100px;" alt="nshCore"/><br /><sub><b>nshCore</b></sub></a><br /><a href="#infra-nshCore" title="Infrastructure (Hosting, Build-Tools, etc)">🚇</a> <a href="#ideas-nshCore" title="Ideas, Planning, & Feedback">🤔</a> <a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=nshCore" title="Tests">⚠️</a> <a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=nshCore" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/franssl"><img src="https://avatars0.githubusercontent.com/u/46971650?v=4" width="100px;" alt="franssl"/><br /><sub><b>franssl</b></sub></a><br /><a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=franssl" title="Tests">⚠️</a> <a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=franssl" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/monsieurleberre"><img src="https://avatars2.githubusercontent.com/u/4638821?v=4" width="100px;" alt="monsieurleberre"/><br /><sub><b>monsieurleberre</b></sub></a><br /><a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=monsieurleberre" title="Tests">⚠️</a> <a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=monsieurleberre" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/atlassanjay"><img src="https://avatars3.githubusercontent.com/u/49910176?v=4" width="100px;" alt="atlassanjay"/><br /><sub><b>atlassanjay</b></sub></a><br /><a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=atlassanjay" title="Tests">⚠️</a> <a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=atlassanjay" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/Millymanz"><img src="https://avatars3.githubusercontent.com/u/5070123?v=4" width="100px;" alt="Millymanz"/><br /><sub><b>Millymanz</b></sub></a><br /><a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=Millymanz" title="Tests">⚠️</a> <a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=Millymanz" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/richardschneider"><img src="https://avatars2.githubusercontent.com/u/631061?v=4" width="100px;" alt="Richard Schneider"/><br /><sub><b>Richard Schneider</b></sub></a><br /><a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=richardschneider" title="Tests">⚠️</a> <a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=richardschneider" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/Xela101"><img src="https://avatars0.githubusercontent.com/u/11431881?v=4" width="100px;" alt="Alex"/><br /><sub><b>Alex</b></sub></a><br /><a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=Xela101" title="Code">💻</a> <a href="https://github.com/catalyst-network/Catalyst.Node/commits?author=Xela101" title="Tests">⚠️</a></td>
  </tr>
</table>

<!-- ALL-CONTRIBUTORS-LIST:END -->
