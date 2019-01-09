// Import net module.
const crypto = require('crypto');
var net = require('net');
var redis = require("redis")
    , subscriber = redis.createClient({
        return_buffers: true
    })
    , publisher  = redis.createClient()
    , keyStore = redis.createClient({
        return_buffers: true
    });

    function hex2a(hexx) {
        var hex = hexx.toString();//force conversion
        var str = '';
        for (var i = 0; (i < hex.length && hex.substr(i, 2) !== '00'); i += 2)
            str += String.fromCharCode(parseInt(hex.substr(i, 2), 16));
        return str;
    }
// Create and return a net.Server object, the function will be invoked when client connect to this server.
var server = net.createServer((socket) => {

    console.log('Client connect. Client local address : ' + socket.localAddress + ':' + socket.localPort + '. client remote address : ' + socket.remoteAddress + ':' + socket.remotePort);

    socket.setEncoding('utf-8');

    socket.setTimeout(1000);

    // When receive client data.
    socket.on('data', (data) => {
        
        var buff = new Buffer(data, 'ascii'); //no sure about this
        var network = buff.slice(0,1);
        var client = buff.slice(1,3);
        var clientVersion = buff.slice(3,4);

        console.log("Network");console.log(network.toString('hex'));
        console.log("client");console.log(hex2a(client.toString('hex')));
        console.log("client version ");console.log(hex2a(clientVersion.toString('hex')));

        publisher.publish("nodes", buff);

        console.log('Receive client send data : ' + buff.toString('hex') + ', data size : ' + socket.bytesRead);    
        
        const hash = crypto.createHash('sha256');

        hash.update(buff.slice(0,43));
    
        keyStore.set(hash.digest('hex'), buff.slice(0,43));
        
        keyStore.get('895e3c0431b5eb6579e89498275d99821a7279f3bfb00cb9f5eae7b04ab35d3f', (err, data) => {
            console.log(err);
            console.log(data);
        })        

        //client.end('Server received data : ' + buff.toString('hex')  + ', send back to client data size : ' + client.bytesWritten);
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
