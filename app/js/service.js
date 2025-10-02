class Service {
	constructor( url, protocols ) {
		this.ws = new WebSocket( url, protocols );
		this._open = new Promise( ( resolve, reject ) => {
			this.ws.addEventListener( "open", resolve, { once: true } );
			this.ws.addEventListener( "error", () => reject( new Error( "WebSocket connect error" ) ), { once: true } );
		});

		this.debug = true;
		this._queue = [];
		this._pending = null;
		this._authenticated = false;

		this.ws.addEventListener( "message", ev => this._onMessage( ev.data ) );
		this.ws.addEventListener( "error", () => this._failAll( new Error( "WebSocket error" ) ) );
		this.ws.addEventListener( "close", () => this._failAll( new Error( "WebSocket closed" ) ) );
	}

	async authenticate() {
		await this._open;
		let token = localStorage.getItem( "authToken" );
		let authreq = {
			module: "auth",
			function: "login",
			data: {
				user: token ? null : "root",
				security: token ? token : "admin?",
				grant_type: token ? "jwt" : "password"
			}
		};
		let authresp = await this._sendRequest( authreq );
		if ( !authresp || authresp.code != 0 ) {
			console.log( "authentication failed" );
			return false;
		} else {
			this._authenticated = true;
			console.log( "service ready" );
			localStorage.setItem( "authToken", authresp.data.auth );
			return true;
		}
	}

	async sendRequest( request ) {
		await this._open;

		if ( !this._authenticated ) {
			await this.authenticate();
		}

		return this._sendRequest( request );
	}

	async _sendRequest( request ) {
		await this._open;
		return new Promise( ( resolve, reject ) => {
			const body = typeof request === "string" ? request : JSON.stringify( request );
			this._queue.push( { request, body, resolve, reject } );
			this._kick();
		});
	}

	_kick() {
		if ( this._pending || this._queue.length === 0 ) {
			return;
		}

		this._pending = this._queue.shift();
		this._debug( "Request: ", Service.plainClone( this._pending.request ) );
		this.ws.send( this._pending.body );
	}

	_onMessage( data ) {
		if ( !this._pending ) {
			return;
		}

		const { resolve } = this._pending;
		this._pending = null;

		let response;
		if ( typeof data === "string" ) {
			try {
				response = JSON.parse( data );
			} catch {
				response = data;
			} finally {
				this._debug( "Response: ", Service.plainClone( response ) );
				resolve( response );
			}
		} else {
			resolve( data );
		}

		this._kick();
	}

	_failAll( err ) {
		if ( this._pending ) {
			this._pending.reject( err );
			this._pending = null;
		}
		while ( this._queue.length ) {
			this._queue.shift().reject( err );
		}
	}

	_debug( ...args ) {
        if ( !this.debug ) {
			return;
		}

        console.log(...args);
    }

    static plainClone(obj) {
        return Object.assign(Object.create(null), obj);
    }
}

const service = new Service("ws://localhost:8080");
