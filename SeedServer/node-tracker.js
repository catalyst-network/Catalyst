// Import net module.
const crypto = require('crypto');
const hexUtil = require('./hexUtil');
var net = require('net');
var redis = require("redis")
    , subscriber = redis.createClient({
    return_buffers: true
})
    , publisher  = redis.createClient()
    , keyStore = redis.createClient({
    return_buffers: true
});

// Create and return a net.Server object, the function will be invoked when client connect to this server.
var server = net.createServer((socket) => {

    console.log('Client connect. Client local address : ' + socket.localAddress + ':' + socket.localPort + '. client remote address : ' + socket.remoteAddress + ':' + socket.remotePort);

socket.setEncoding('utf-8');

socket.setTimeout(1000);

// When receive client data.
socket.on('data', (data) => {

    var buff = new Buffer(data, 'ascii');
    var network = buff.slice(0,1);
    var client = buff.slice(1,3);
    var clientVersion = buff.slice(3,5);
    var ip = buff.slice(5,21);
    var port = buff.slice(21,23);
    var pubkey = buff.slice(21,44);
    
    var byteCount = 0;
    for (var i = 0; i < 11; i++) {
        byteCount + ip[i];
        console.log(byteCount); 
    }
    if (byteCount == 0) {
        var ipv4Bytes = ip.slice(12, 16);
        ip = ipv4Bytes.join('.');
    }
    
    console.log(ip);
    
    var nodeId = {
        network: network.toString('hex'),
        client: hexUtil.decodeHex(client.toString('hex')),
        clientVersion: hexUtil.decodeHex(clientVersion.toString('hex')),
        ip: ip.toString('hex'),
        port: parseInt(port.toString('hex'),16).toString(10),
        pubKey: hexUtil.decodeHex(pubkey.toString('hex'))
    }
    
    const hash = crypto.createHash('sha256');
    const nodeIdHash = hash.digest('hex');
    nodeId.hash = nodeIdHash;
    const payload = JSON.stringify(nodeId);
    publisher.publish("nodes", JSON.stringify({ nodeIdHash, nodeId }));
    keyStore.set(nodeIdHash, payload);
    
    // console.log(nodeId)
    // console.log('Receive client send data : ' + buff.toString('hex') + ', data size : ' + socket.bytesRead);

});

// When client send data complete.
socket.on('end',  () => {
    console.log('socket disconnect.');

// Get current connections count.
server.getConnections(function (err, count) {
    if(!err)
    {
        // Print current connection count in server console.
        console.log("There are %d connections now. ", count);
    }else
    {
        console.error(JSON.stringify(err));
    }

});
});

// When client timeout.
socket.on('timeout', () => {
    console.log('Client request time out. ');
})
});

// Make the server a TCP server listening on port 9999.
server.listen(3030, () => {

    // Get server address info.
    var serverInfo = server.address();

var serverInfoJson = JSON.stringify(serverInfo);

console.log('TCP server listen on address : ' + serverInfoJson);

server.on('close', () => {
    console.log('TCP server socket is closed.');
});

server.on('error', (error) => {
    console.error(JSON.stringify(error));
});

subscriber.on("message", function(channel, message) {
    console.log("Message '" + message.toString('hex') + "' on channel '" + channel + "' arrived!")
});

subscriber.subscribe("nodes");

});
