var Room = require('colyseus').Room;
var base = require('base-converter')
var md5 = require('md5')

class ChatRoom extends Room {

    constructor() {
        super();
        // Maximum number of clients who can join a single room
        this.maxClients = 2;
        this.setState({
            players: {},
            pwd: ''
        });
    }

    onInit(options) {
        this.setPatchRate(1000 / 20);
        this.setSimulationInterval(this.update.bind(this));

        console.log("ChatRoom created!", options);
    }

    /**
     * Requesting to join the room with options. For this example, options.type can has values 'joinPublic', 'joinPrivate' and 'createPublic'.
     * @param {any} options The options sent from the client with appended clientId and sessionId
     */
    requestJoin(options) {
        console.log("request join!", options);
        if (options && options.type) {
            switch (options.type) {
                // A client can join a public game whenever a password is not set and the game room is not ready to play 
                case 'joinPublic':
                    {
                        console.log('join public ' + options)
                        if (this.state.ready === 1) {
                            console.log('maximum players reached')
                            return false
                        }
                        if (this.state.pwd !== '') {
                            console.log('password already set')
                            return false
                        }
                        console.log(this.state)
                        return true
                    }
                    break
                // A client can join a private game whenever the password matches with the given password and game room is not ready to play 
                case 'joinPrivate':
                    {
                        console.log('trying to join private')
                        if (this.state.ready !== 1 && options.gameID && options.gameID === this.state.pwd) {
                            console.log(this.state)
                            return true
                        }
                        console.log('unsuccessfull')
                        return false
                    }
                    break
                /**
                 *  When creating a private game, the password is set by the client ID who created the game. The client IDs are always unique.
                 *  Thefore the passwords too.
                 */
                case 'createPrivate':
                    {
                        console.log('trying to create private')
                        if (this.clients.length === 0) {
                            this.state.pwd = base.decTo62(parseInt(md5(options.clientId), 16)).substr(0, 8)
                            console.log('Password is ', this.state.pwd)
                            return true
                        }
                        console.log('failed')
                        return false
                    }
                    break
            }
        }
        return false

    }

    /**
     * Called after joining the room
     * @param {Client} client The joined client
     * @param {any} options Original options used while requesting the join without clientId and sessionId
     */
    onJoin(client, options) {
        console.log("client joined!", client.id);

        // Creating the player object in the state variable
        this.state.players[client.id] = {
            id: client.id,
            name: options.name,
            x: 0,
            y: 0,
            color: 1
        };

        // If a password set ie. the game is private, server sends the credentials to the creator client  
        if (this.state.pwd !== '') {
            this.send(this.clients[0], {
                command: 'privateGameID',
                privateGameID: this.state.pwd
            })
        }

        // If the maximum clients are connected, the game is started
        if (Object.keys(this.state.players).length === this.maxClients) {
            this.state.ready = 1;
            this.startGame();
        }
    }

    /**
     * Called when the client left the game
     * @param {Client} client Leaving client
     */
    onLeave(client) {
        console.log("client left!", client.id);

        // Making the other client aware that the opponnent left
        if (this.clients.length === 1) {
            this.send(this.clients[0], {
                command: 'oppLeft'
            })
        }
        delete this.state.players[client.id];
    }

    /**
     * Called when a client sends a message to the server. For this example, data.type is necessary to identify the type of the message.
     * @param {Client} client Sender client
     * @param {any} data Data
     */
    onMessage(client, data) {
        console.log(data, "received from", client.id);
        if (data.type) {
            switch (data.type) {
                // When a user changes his color
                case 'color':
                    {
                        this.state.players[client.id].color = data.color;
                    }
                    break
                // When a user adds a message
                case 'message':
                    {
                        data.command = 'message'
                        data.id = client.id
                        console.log(data)
                        this.broadcast(data)
                    }
                    break
                // When a user changes his position
                case 'move':
                    {
                        this.state.players[client.id].x = data.x;
                        this.state.players[client.id].y = data.y;
                    }
            }
        }
        console.log(this.state)
    }

    /**
     * Sending data to a specific client
     * @param {Client} client The target reciever
     * @param {any} data Data 
     */
    send(client, data) {
        super.send(client, data)
    }
    

    /**
     * Sending data to all connected clients of the room
     * @param {any} data Data
     */
    broadcast(data) {
        super.broadcast(data)
    }

    /**
     * Called in the rate of the patch rate. For this example, this is not used.
     */
    update() {
    }

    /**
     * Called when the room is abandoned ie. this.clients.length === 0
     */
    onDispose() {
        console.log("Dispose ChatRoom");
    }

    /**
     * Used in this example to start the game
     */
    startGame() {
        console.log('starting game', this.state)
        for (var i = 0; i < 2; i++) {
            this.state.players[this.clients[i].id].x = 1.5 * (2 * i - 1);
            this.state.players[this.clients[i].id].color = i;

            // duplicating the player data to send
            var playerData = JSON.parse(JSON.stringify(this.state.players[this.clients[i].id]))
            playerData.command = 'playerData'

            this.broadcast(playerData)
        }
    }

}

module.exports = ChatRoom;