var express = require('express');
var app = express();
var http = require('http').Server(app);

var redis = require('redis')
  , subscriber = redis.createClient({
    return_buffers: true
  })
  , publisher  = redis.createClient()

var io = require('socket.io')(http)

io.on('connection', function(socket) {
  console.log(socket.id)
});

app.use('/', express.static('www'));

app.use(function(req, res, next) {
  console.log(req);
  res.header("Access-Control-Allow-Origin", "*");
  res.header("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");
  next();
});

http.listen(8000, function(){
    console.log('listening on *:8000');
});

subscriber.subscribe('nodes');

subscriber.on('message', (chan, msg) => {
  console.log(msg)

  io.sockets.emit('node_announce', msg);

  // publisher.hgetall(msg, (err, res) => {
  //   res.key = msg;
  //   io.sockets.emit('node-announce', res);
  // });
});


subscriber.subscribe('nodes');