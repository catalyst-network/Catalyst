# Seed Node

A seed node is a well known node that allows other 
[IPFS nodes](https://richardschneider.github.io/net-ipfs-engine/articles/intro.html) to discover each other. In IPFS-speak this is 
a bootstrap node.

Catalyst uses an [IPFS private network](https://richardschneider.github.io/net-ipfs-engine/articles/pnet.html)
that only allows
communication among catalyst nodes.

## Usage

```
> dotnet Catalyst.SeedNode.dll --help
Catalyst.SeedNode 0.0.1
Copyright © 2019 AtlasCity.io

  -p, --ipfs-password    The password for IPFS.  Defaults to prompting for the password.
  --help                 Display this help screen.
  --version              Display version information.
```