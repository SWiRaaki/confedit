
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public record Error( [property:JsonProperty("code")] long Code, [property:JsonProperty("msg")] string Message );

public class Request {
	[JsonProperty("module")]
	public string Module { get; set; }

	[JsonProperty("function")]
	public string Function { get; set; }

	[JsonProperty("data")]
	public object Data { get; set; }
}

public class Response {
	[JsonProperty("module")]
	public string Module { get; set; }

	[JsonProperty("code")]
	public long Code { get; set; }

	[JsonProperty("data")]
	public object? Data { get; set; }

	[JsonProperty("errors")]
	public List<Error> Errors { get; set; } = new();
}

public class Client {
	public Client( Guid id, WebSocket socket ) {
		ID = id;
		mySocket = socket;
	}

	public async Task Handle() {
		var buffer = new byte[4096]; // 4KB buffer
		var offset = 0;
		var free = buffer.Length;

        try
        {
            while (mySocket.State == WebSocketState.Open)
            {
				WebSocketReceiveResult result = await mySocket.ReceiveAsync( new ArraySegment<byte>( buffer, offset, free ), CancellationToken.None );
				offset += result.Count;
				free -= result.Count;
				while( !result.EndOfMessage ) {
					if ( free == 0 ) {
						var newSize = buffer.Length + 4096;
						var newBuffer = new byte[newSize];
						Array.Copy( buffer, 0, newBuffer, 0, offset );
						buffer = newBuffer;
						free = buffer.Length + offset;
					}
					result = await mySocket.ReceiveAsync( new ArraySegment<byte>( buffer, offset, free ), CancellationToken.None );
				}
                if ( result.MessageType == WebSocketMessageType.Text )
                {
                    string message = Encoding.UTF8.GetString( buffer, 0, offset );
                    Console.WriteLine( $"[{ID}]: Received '{message}'" );
					var request = JsonConvert.DeserializeObject<Request>( message );
					var response = new Response();
					if ( request == null ) {
						Console.WriteLine( "Failed to convert request" );
					}
					HandleRequest( request, out response );
					message = JsonConvert.SerializeObject( response );
                    byte[] responseBytes = Encoding.UTF8.GetBytes( message );
                    await mySocket.SendAsync( new ArraySegment<byte>( responseBytes ), WebSocketMessageType.Text, true, CancellationToken.None );
					Console.WriteLine( $"[{ID}]: Responded: '{message}'" );
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine($"Client {ID} disconnected.");
                    break;
                }
				Array.Clear( buffer );
				offset = 0;
				free = buffer.Length;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error with client {ID}: {ex.Message}");
        }
        finally
        {
            Program.Clients.TryRemove(ID, out _);
            await mySocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            Console.WriteLine($"Client {ID} removed. Active clients: {Program.Clients.Count}");
        }
	}

	void HandleRequest( Request request, out Response response ) {
		response = new Response() {
			Module = request.Module,
			Code = -1,
			Data = null,
			Errors = {
				new Error(-1, $"{request.Module}.{request.Function} is not a function ( May be WIP )" )
			}
		};
	}

	public Guid ID { get; private set; }
	
	private WebSocket mySocket;
}
