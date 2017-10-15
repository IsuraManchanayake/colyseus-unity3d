var Room = require('colyseus').Room;

class ChatRoom extends Room {
    
    constructor () {
        super();
        this.maxClients = 2;
        this.setState({
            players: {}
        });
    }
    
    onInit (options) {
        this.setPatchRate( 1000 / 20 );
        this.setSimulationInterval( this.update.bind(this) );
        
        console.log("ChatRoom created!", options);
    }
    
    requestJoin (options) {
        console.log("request join!", options);
        return true;
    }
    
    onJoin (client, options) {
        console.log("client joined!", client.id);
        this.state.players[client.id] = { id: client.id, name: options.name, x: 0, y: 0, color: 1 };
        if(Object.keys(this.state.players).length === this.maxClients) {
            this.startGame();
        } 
    }
    
    onLeave (client) {
        console.log("client left!", client.id);
        delete this.state.players[client.id];
    }
    
    onMessage (client, data) {
        console.log(data, "received from", client.id);
        if(data.type) {
            switch(data.type) {
                case 'color': {
                    this.state.players[client.id].color = data.color;
                }
                break
                case 'message': {
                    data.command = 'message'
                    data.id = client.id
                    console.log(data)
                    this.broadcast(data)
                }
                break
                case 'move': {
                    this.state.players[client.id].x = data.x;
                    this.state.players[client.id].y = data.y;
                }
            }
        }
        console.log(this.state)
    }
    
    update () {
        // console.log(Object.keys(this.state.players))
        // if(Object.keys(this.state.players).length > 0) { 
        //   this.state.players[this.clients[0].id].y++;
        // }
    }
    
    onDispose () {
        console.log("Dispose ChatRoom");
    }
    
    startGame() {
        console.log('starting game', this.state)
        for(var i = 0; i < 2; i++) {
            this.state.players[this.clients[i].id].x = 1.5 * (2 * i - 1);
            this.state.players[this.clients[i].id].color = i;
            var playerData = JSON.parse(JSON.stringify(this.state.players[this.clients[i].id]))
            playerData.command = 'playerData'
            // this.send(this.clients[1 - i], oppData)
            this.broadcast(playerData)
        }
    }
    
}

module.exports = ChatRoom;