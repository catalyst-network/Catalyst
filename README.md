# <p style="text-align: center;"> Catalyst Network </p>

|Windows Build   |OSX Build   |Linux Build   | Code Quality
|:-:	|:-:	|:-:	|:-:	|
|[![Build Status](https://dev.azure.com/AtlasCityIO/catalyst-network/_apis/build/status/catalyst-network-windows-build-develop?branchName=develop)](https://dev.azure.com/AtlasCityIO/catalyst-network/_build/latest?definitionId=3&branchName=develop)   	|[![Build Status](https://dev.azure.com/AtlasCityIO/catalyst-network/_apis/build/status/catalyst-network-osx-build-develop?branchName=develop)](https://dev.azure.com/AtlasCityIO/catalyst-network/_build/latest?definitionId=2&branchName=develop)   	|[![Build Status](https://dev.azure.com/AtlasCityIO/catalyst-network/_apis/build/status/catalyst-network-linux-build-develop?branchName=develop)](https://dev.azure.com/AtlasCityIO/catalyst-network/_build/latest?definitionId=1&branchName=develop)   	|[![Codacy Badge](https://api.codacy.com/project/badge/Grade/0940fa58afc24dbf96ad566f1fdc1390)](https://www.codacy.com?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=catalyst-network/Catalyst.Node&amp;utm_campaign=Badge_Grade)	|

<hr/>

## What is the Catalyst Network ?

Catalyst Netowkr is the distributed operating system that enterprise and developers can get straight on. No language barriers.  No architecture constraints. Scalable, fast and lean. The first DLT that is fit for purpose.

The decentralised protocol is designed from engineering first principles and built by an experienced team of software developers and financial service experts. With an enterprise focus it allows industries to leverage decentralisation into their operations.

## What is Catalyst.Node ?

Catalyst.Node is the .Net implementation of the Catalyst Network. The spcification of our protocol is outlined in our technical while paper a more indepth implementation orientated document can be found in the "[Protocol Blueprint]"(https://github.com/catalyst-network/protocol-blueprint/)

Catalyst.Node is developed on dotnet core 2.2 therefore supports Linux, macOS and Windows operating systems.

## Documentation

Is coming (don't you hate it when they say that). For now you can read through the project [wiki](https://github.com/catalyst-network/Catalyst.Node/wiki) or the [Protocol Blueprint](https://github.com/catalyst-network/protocol-blueprint/)

## Features

- Protobuffs wire format ([see why](https://github.com/catalyst-network/protocol-protobuffs#why-protobuffs))
- RPC core protocol methods
- Distributed file storage (DFS) built upon IPFS
- Confidential and Public transactions
- Fast new and novel consensus Delegated Proof Of Real Work With Some Staking & Reputation Elements (Cheers for the unhelpfull answer here @TonyVazz)
- Flexible modula design

## Real Quick Start Guide

`git clone git@github.com:catalyst-network/Catalyst.Node.git `
`cd Catalyst.Node`
`git submodule init`
`git submodule update`
`cd src`
`dotnet restore`
`dotnet build`

If that was really too quick try reading the [Quick Start Guide](https://github.com/catalyst-network/Catalyst.Node/wiki/Quick-Start-Guide/_edit)
