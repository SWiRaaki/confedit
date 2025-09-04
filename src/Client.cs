using System.Linq.Expressions;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

internal record ReadResult( WebSocketMessageType Type, byte[] Data );

internal class Client {
	internal Client( WebSocket socket ) {
		mySocket = socket;
	}

	internal async Task Handle() {
        try
        {
            while ( mySocket.State == WebSocketState.Open )
            {
				ReadResult result = await ReadMessage();
                if ( result.Type == WebSocketMessageType.Text )
                {
					string text = Encoding.UTF8.GetString( result.Data );
                    Console.WriteLine( $"[{ID}]: Received '{text}'" );
					Request request = JsonConvert.DeserializeObject<Request>( text ) ?? new();
					Response response;
					HandleRequest( request, out response );

					text = JsonConvert.SerializeObject( response );
                    byte[] responseBytes = Encoding.UTF8.GetBytes( text );
                    await mySocket.SendAsync( new ArraySegment<byte>( responseBytes ), WebSocketMessageType.Text, true, CancellationToken.None );
					Console.WriteLine( $"[{ID}]: Responded: '{text}'" );
                }
				else if ( result.Type == WebSocketMessageType.Binary ) {
					Console.WriteLine( $"[{ID}]: Received {result.Data.Length} bytes as binary. Unsupported, ignored!" );
					var response = new Response() {
						Module = "ce",
						Code = -1,
						Errors = {
							new Error(-1, "Binary requests are not supported!" )
						}
					};

					string text = JsonConvert.SerializeObject( response );
					byte[] responseBytes = Encoding.UTF8.GetBytes( text );
					await mySocket.SendAsync( new ArraySegment<byte>( responseBytes ), WebSocketMessageType.Text, true, CancellationToken.None );
					Console.WriteLine( $"[{ID}]: Responded: '{text}'" );
				}
                else if ( result.Type == WebSocketMessageType.Close )
                {
                    Console.WriteLine( $"Client {ID} disconnected." );
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine( $"Error with client {ID}: {ex.Message}" );
        }
        finally
        {
            Program.Clients.TryRemove( ID, out _ );
            await mySocket.CloseAsync( WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None );
            Console.WriteLine( $"Client {ID} removed. Active clients: {Program.Clients.Count}" );
        }
	}

	void HandleRequest( Request request, out Response response ) {
		Module? module;
		ModuleProcess? function;
		if ( !Program.Module.TryGetValue( request.Module, out module ) ) {
			response = new Response() {
				Module = "ce",
				Code = -1,
				Errors = {
					new Error( -2, $"{request.Module} is not a module ( May be WIP )" )
				}
			};
		} else if ( !module.Function.TryGetValue( request.Function, out function ) ) {
			response = new Response() {
				Module = "ce",
				Code = -1,
				Errors = {
					new Error( -2, $"{request.Module}.{request.Function} is not a function ( May be WIP )" )
				}
			};
		} else {
			function( this, request, out response );
		}
	}

	internal async Task<ReadResult> ReadMessage() {
		var binary = new byte[4096]; // 4KB buffer
		var offset = 0;
		var free = binary.Length;
		
		WebSocketReceiveResult result = await mySocket.ReceiveAsync( new ArraySegment<byte>( binary, offset, free ), CancellationToken.None );
		offset += result.Count;
		free -= result.Count;
		if (result.MessageType == WebSocketMessageType.Close)
		{
			return new ReadResult( WebSocketMessageType.Close, binary );
		}
		while( !result.EndOfMessage ) {
			if ( free == 0 ) {
				var newSize = binary.Length + 4096;
				var newBuffer = new byte[newSize];
				Array.Copy( binary, 0, newBuffer, 0, offset );
				binary = newBuffer;
				free = binary.Length - offset;
			}
			result = await mySocket.ReceiveAsync( new ArraySegment<byte>( binary, offset, free ), CancellationToken.None );
		}
		return new ReadResult( result.MessageType, binary );
	}

	internal async Task WriteMessage( string message ) {
		var binary = Encoding.UTF8.GetBytes( message );
		await mySocket.SendAsync( new ArraySegment<byte>( binary ), WebSocketMessageType.Text, true, CancellationToken.None );
	}

	internal async Task<bool> Handshake() {
		ReadResult result = await ReadMessage();
		if ( result.Type != WebSocketMessageType.Text )
			return false;
		try
		{
			Request request = JsonConvert.DeserializeObject<Request>( Encoding.UTF8.GetString( result.Data ) ) ?? new();
			Response response;

			bool ret = Program.Module["auth"].Function["login"]( this, request, out response );
			await WriteMessage( JsonConvert.SerializeObject( response ) );
			return ret;
		}
		catch ( Exception e )
		{
			Console.WriteLine( $"Failed to parse or call request: {e.Message}" );
			return false;
		}
	}

	internal Guid ID { get; set; }
	
	private WebSocket mySocket;
}
