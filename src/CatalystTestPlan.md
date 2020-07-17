# Catalyst Test Plan
> This Test Plan describes the integration and system tests that will be conducted on Catalyst following integration of the subsystems and components identified.

> It is assumed that unit testing already provided thorough black box testing, extensive coverage of source code, and testing of all module interfaces.

> The testing will revolve around Catalyst features in a live network, performance tests are currently excluded from this test plan.

The following interfaces will be tested or used for testing:
-  Catalyst Node
 
-  Catalyst Dashboard
 
-  Truffle - HDWalletProvider
 
> The tests should be conducted on both local and remote computers.

## Test Cases

### Consensus
To test the consensus is working correctly - start a few POA nodes and let them produce new deltas, every node should eventually produce a new delta as long as it is a POA node. The cycle’s should always be in-sync and the delta production time should always be +19 seconds. 
> You can view the delta production in the block explorer.

### Synchronization
To test synchronization with other remote peers:
Start a few POA nodes and let them run and participate in consensus until over a hundred blocks are generated.

POA Node - Start a new POA node until it synchronizes to the current block height of the other nodes, if it completes and continues to participate in consensus(Receives deltas from other nodes and produces its own deltas to other nodes) it has successfully synchronized.

Non POA Node - Start a new non POA node wait until it synchronizes to the current block height of the other nodes, if it completes and continues receiving new blocks every cycle the synchronization process has successfully completed.

### Transactions/Smart Contracts
The truffle tests will generate plain transactions as well as smart contract transactions. 
To test transactions and smart contracts:

Edit the file "src\Catalyst.Core.Modules.Ledger.Tests\IntegrationTests\TruffleTest\truffle-config.js" and point the catalyst endpoint to the node you would like to test. Run the tuffle tests from console: 
```truffle test --network catalyst```

You should eventually receive transactions that contain plain transactions as well as smart contract transactions.

### Web3
The Web3 controller has two main functions, an interface to both the DFS and EVM as well as custom controllers created by the node operator.
To test the DFS and custom controllers this can be done through the swagger endpoint at: `http://[IPAddress]:5005/swagger/index.html`

> To test the EVM json rpc this can be tested at: `http://[IPAddress]:5005/api/eth/request`

> For a list of eth json rpc calls you can use the following site: `https://infura.io/docs/ethereum/json-rpc/eth-call`
