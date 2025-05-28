using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;

static class Program 
{
	static Program() 
	{
		Clients = new();
		Listener = new();
	}

	static async Task Main( string[] args ) 
	{

		Console.WriteLine( "ConfE dit Server V0.1.0 startup.." );

		Listener.Prefixes.Add( "http://localhost:42069/" );
		Listener.Start();

		while ( true ) 
		{
			var context = await Listener.GetContextAsync();
			if (context.Request.IsWebSocketRequest)
			{
				var wsContext = await context.AcceptWebSocketAsync(null);
				WebSocket webSocket = wsContext.WebSocket;
				Guid clientId = Guid.NewGuid();

				CEClient client = new CEClient( clientId, webSocket );
				Clients.TryAdd( clientId, client );
				Console.WriteLine($"Client connected: {clientId} (Total: {Clients.Count})");

				_ = client.Handle();
			}
			else
			{
				context.Response.StatusCode = 400;
				context.Response.Close();
			}
		}
	}

	public static ConcurrentDictionary<Guid, CEClient> Clients { get; private set; }
	public static HttpListener Listener { get; private set; }
}