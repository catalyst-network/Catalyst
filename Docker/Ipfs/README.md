# docker-ipfs
This is a collection of files and configuration for playing with IPFS
in Docker. Most of the experimentation will be focused on private networks and
clustering.

## Starting points
### Resources
Good explanation for the role of private networks vs clustering
<pre>They are separate features/functionality.

* With private networks each node specifies which other nodes it will connect to. Nodes in that network don't respond to communications from nodes outside that network.
* With ipfs-cluster you use a leader-based consensus algorithm to coordinate storage of a pinset -- distributing the set of data across the participating nodes based on whichever pattern you prefer

You could use these features together -- using ipfs-cluster to spread a pinset across a private network of nodes -- but they are completely separate features. They do not rely on each other. Support for private networks is functionality implemented within the core (go-ipfs) code base. ipfs-cluster is its own separate code base.
</pre>

-- **flyingzumwalt**

https://discuss.ipfs.io/t/how-to-create-a-private-network-of-ipfs/339/7

#### Clustering
* https://github.com/ipfs/ipfs-cluster/blob/master/docs/ipfs-cluster-guide.md
* https://discuss.ipfs.io/t/solved-need-help-to-setup-a-new-ipfs-cluster-with-2-peers/835

### Private networks
* https://github.com/ipfs/go-ipfs/blob/master/docs/experimental-features.md#private-networks

## Private network setup
### Generate a swarm key
If you don't have the `ipfs-swarm-key-gen` binary locally, you can use Docker to
to fetch the dependency and generate a swarm key by running the following:
```bash
$ docker run --rm golang:1.9 sh -c 'go get github.com/Kubuxu/go-ipfs-swarm-key-gen/ipfs-swarm-key-gen && ipfs-swarm-key-gen'
/key/swarm/psk/1.0.0/
/base16/
f744ccf21ef090407977a33e01deb0a0c6a3397ae0366ff6f3c749e200f2510d
```

### Simple way to run a private network
```bash
docker run --rm -e LIBP2P_FORCE_PNET=1 -e SWARM_KEY="/key/swarm/psk/1.0.0/\n/base16/\ne0e7b1394fb6e928eecf2f8db77eaa99d3657684dc939519f285cb902bd93e22" -v ./private-network/init.sh:/usr/local/bin/start_ipfs ipfs/go-ipfs:latest
```
