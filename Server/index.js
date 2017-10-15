"use strict";

var colyseus = require('colyseus')
  , http = require('http')

  , express = require('express')

  , port = process.env.PORT || 3553
  , host = '127.0.0.1'
  , app = express()
  , server = http.createServer(app)
  , gameServer = new colyseus.Server({ server: server })

  , ChatRoom = require('./chat_room');

gameServer.register('chat', ChatRoom)

app.use(express.static(__dirname))
gameServer.listen(port, host);

console.log(`Listening on http://${host}:${port}`)
