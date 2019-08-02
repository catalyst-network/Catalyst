# Seed Node

A seed node is a well known node that allows other 
[IPFS nodes](https://richardschneider.github.io/net-ipfs-engine/articles/intro.html) to discover each other. In IPFS-speak this is 
a bootstrap node.

Catalyst uses an [IPFS private network](https://richardschneider.github.io/net-ipfs-engine/articles/pnet.html)
that only allows
communication among catalyst nodes.

## Usage

```console
> dotnet Catalyst.SeedNode.dll --help
Catalyst.SeedNode 0.0.1
Copyright © 2019 AtlasCity.io

  -p, --ipfs-password    The password for IPFS.  Defaults to prompting for the password.
  --help                 Display this help screen.
  --version              Display version information.
```

## Addresses

The [multi-addresses](https://richardschneider.github.io/net-ipfs-core/articles/multiaddress.html) of the seed nodes, as of 30 July 2019

```console
/ip4/165.22.209.154/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZdzVq4jUMZsJKddJPrwWjkcwtf23ZcGFW2xUCmSE29ABRs
/ip4/165.22.226.50/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZe2jBdLcqbqE6qLfApXJLPr855vycKygWXnRsMVXuW8o1E
/ip4/167.71.129.154/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZe79QPeKgXsvTvVTPV732TnSya3MqQ5YUMcGHZFxW1VAEC
/ip4/167.71.79.54/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZe4BowwZZyjRazPAMd5VhWBfvM88Qcyy3viWGQ8fzEhcvc
/ip4/167.71.79.77/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZe64FqVyvgWoLYBktgShFe6oEYeDWox8o2WhA8brQu5dBA
/ip4/165.22.175.71/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZdwHHKV44GJfKE9ecyRXHcXY4yNtEExgPSvvwsDtvZspZj
/ip4/165.22.234.49/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZdxSv6GpHsHAVYb51xvH3y4xvGVztM1h1GEsUjEzqc8Wh4
/ip4/167.71.134.31/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZe4SkF2tXtZbBMUSsmbosM5SAJaU3oxUAbReFDBaKeHqR3
```