# Command Line Options
Catalyst CLI commands list (version 0.0.1).

Command options are denoted inside < and > arguments are inside [ and ].

|  Command  |  Parameters    | Description                                                                           |
|---------|----------------|---------------------------------------------------------------------------------------|
|connect| [node Id] |Connects the CLI to a running catalyst node.<br>Accepts the node id as defined in the devnet.json file.|
|getinfo| [node Id] |Returns the node configuration. <br>Accepts the node id as defined in the devnet.json file.|
|getversion| [node Id] |Returns the node version. <br>Accepts the node id as defined in the devnet.json file.|
|getmempool| [node Id]| Returns the list of transactions in the memory pool. <br>Accepts the node id as defined in the devnet.json file.|
|sign| <-m> [text message] <-n> < node ID> | Signs a message and returns the following:<br>- Signature<br>- Public Key<br>- Original Message.<br>Accepts the text message to be signed and the node id as defined in the devnet.json file.|
|verify|<-m> [text_message] <-s> [signature] -k [public_key] <-n> [node ID]| Verify a message and returns True/False based on the verification result.<br>Accepts the following:<br> - The signed text message<br>- Signature<br>- Public Key <br>- The node id as defined in the devnet.json file.|
|listpeers|<-n> [node Id]|Returns the list of peer nodes. <br>Accepts the node id as defined in the devnet.json file.|
|peercount|<-n> [node Id]|Returns the number of peer nodes. <br>Accepts the node id as defined in the devnet.json file.|
|removepeer|<-n> [node Id] <-k> [Public Key] <-i> [IP Address]|Removes a peer from the list of peers.<br>Accepts the following:<br> - The public key of the peer to be removed<br>- IP address of the peer<br>- The node id as defined in the devnet.json file.|
|peerrep|<-n> [node Id] <-l> [IP Address] <-p> [Public Key]|Returns the reputation of a peer.<br>Accepts the following:<br> - The public key of the peer to be removed<br>- IP address of the peer<br>- The node id as defined in the devnet.json file.|
|addfile|<-n> [node Id] <-f> [File Name]|Uploads a file to DFS and returns the following if the file is successfully uploaded:<br>- Response Status: Finished<br>- The DFS hash<br>Otherwise, one of the following responses is returned:<br>- TransferPending<br>- Error<br>- Expired<br>- Failed<br>Accepts the following:<br> - The full qualified name of the file<br>- The node id as defined in the devnet.json file.|
|getfile|<-n> [node Id] <-f> [DFS Hash] <-o> [Output path]|Gets a file from DFS.<br>Accepts the following:<br> - The full qualified name of the output file<br>- The DFS hash<br>- The node id as defined in the devnet.json file.|