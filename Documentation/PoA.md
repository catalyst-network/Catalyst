# Catalyst Proof of Authority consensus mechanism

> Intend to be used in private blockchain setups, this consensus mechanism is a simplified version of the innovative consensus mechanism developed for the Catalyst network.  
> In this simplified version 

## Election of the authorities 

In this consensus algorithm, all nodes are both block producers and validators and have an equal chance to be chosen for the production of the next [_delta_](https://github.com/catalyst-network/protocol-blueprint/blob/master/Deltas.md). 
> As an approximation for this proof of concept, we are assuming that the list of nodes on the network is small and 
stable enough to be known at all time by all participants.

As a new block is produced and identified by its unique hash _h_, a new set of distinct hashes can be produced by hashing the
identifiers of all the nodes present at that point on the network 
(Cf. [peerId](https://github.com/catalyst-network/protocol-blueprint/blob/master/PeerProtocol.md#peer-identifier))
and the hash of the new block.

This new set of hashes is then ordered and used to sort the block producers in order of preference. For example, the node
with the [peerId](https://github.com/catalyst-network/protocol-blueprint/blob/master/PeerProtocol.md#peer-identifier) that
produces the lowest hash can be considered the favorite for the production of the next block, while the one with the highest
would be the least preferred.

## Production, voting, and synchronisation phases

Each _cycle_ resulting in the addition of a new [delta](https://github.com/catalyst-network/protocol-blueprint/blob/master/Deltas.md)
to the chain is divided into 2 distinct parts
- A production phase, during which the producers elaborate the content of the next [delta](https://github.com/catalyst-network/protocol-blueprint/blob/master/Deltas.md)
- A voting phase, during which the validators examine the content of the deltas they received and score them in order of preference
> - A synchronisation phase where ????

### Production phase

1. All nodes on the network are meant to produce their own version of the new 
[delta](https://github.com/catalyst-network/protocol-blueprint/blob/master/Deltas.md)
based on the content of their transaction pool (_mempool_) at the beginning of the production cycle.
2. Each node then submits their own version of the next 
[delta](https://github.com/catalyst-network/protocol-blueprint/blob/master/Deltas.md) to the rest of 
the network.

### Validation phase

1. Each candidate [Delta](https://github.com/catalyst-network/protocol-blueprint/blob/master/Deltas.md) is ranked by _popularity_:
the further their content is from the most popular content, the lower its score is.
2. Each candidate [Delta](https://github.com/catalyst-network/protocol-blueprint/blob/master/Deltas.md) is ranked by its producer's _authority_ (cf. election process):
the delta produced by the preferred producer will score the highest, while the delta produced by the least preferred producer will score the lowest.
3. The 2 previous scores are then weighted and summed together to find the final ranking of each block.

### Synchronisation phase

> ??? the highest scoring candidate emerges as the next block but I don't exactly remember how.