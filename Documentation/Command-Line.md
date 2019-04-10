# Command Line Options
Catalyst CLI commands list (version 0.0.1).

Required arguments are denoted inside < and > Optional arguments are inside [ and ].

|  Command  |  Parameters    | Description                                                                           |
|:---------:|----------------|---------------------------------------------------------------------------------------|
|  connect  | <-n> < node ID > | Connects the CLI to a catalyst node. The value is the node id.                        |
|  get      | < option >  < node ID >| Option can be:<br>-i: Returns node configuration. Returns devnet.json file contents. The value is the node id. <br>-m: Returns the contents of the node mempool. The value is the node id.<br>-v: Returns the node version. The value is the node id.|
|  sign     | <-m> < text message > <-n>  < node ID> | Signs the message entered. The value is the node id. Returns the following:<br>1. Signature<br>2. Public Key<br>3. Original Message|