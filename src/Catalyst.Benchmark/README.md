<div align="center">
  <img alt="ReDoc logo" src="https://raw.githubusercontent.com/catalyst-network/Community/master/media-pack/logo.png" width="400px" />

  ### Crypto Benchmarking
 
[![Discord](https://img.shields.io/discord/629667101774446593?color=blueviolet&label=discord)](https://discord.gg/anTP7xm)
![GitHub followers](https://img.shields.io/github/followers/catalyst-network?style=social)
[![GitHub stars](https://img.shields.io/github/stars/catalyst-network/community?style=social)](https://github.com/catalyst-network/protocol-protobuffs/stargazers)
[![Twitter Follow](https://img.shields.io/twitter/follow/catalystnetorg?style=social)](https://twitter.com/catalystnetorg)
[![Subreddit subscribers](https://img.shields.io/reddit/subreddit-subscribers/catalystnet?style=social)](https://reddit.com/r/catalystnet)
</div>

Benchmarker for various crypto libraries

## Setup
build:
```shell
dotnet publish -c Release -o out
```

## Run benchmarks

To run all benchmarks and collate them into a single table:
```shell
dotnet out/Catalyst.Benchmark.dll -f '*' --join
```

To run single point of comparison eg benchmark the verification method of all libraries:
```shell
dotnet out/Catalyst.Benchmark.dll --anyCategories=verify —-join
```

To compare just ed25519 methods (or secp256k1)
```shell
dotnet out/Catalyst.Benchmark.dll --anyCategories=ed25519 --join
```
To get info about memory allocation add ```-m``` to the console arguments

To run tests for a single library
```shell
dotnet out/Catalyst.Benchmark.dll
```
to get console options

You can also use ```Catalyst.Benchmark.exe``` tool to run benchmarks.


## Reports

Reports can be found in the BenchmarkDotNet.Artifacts/results folder.

[Report 4/1/19](BenchmarkDotNet.Artifacts/results/BenchmarkRun-joined-2019-01-04-01-35-42-report-github.md)

