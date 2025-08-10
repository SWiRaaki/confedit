using System.Collections.Concurrent;
using System.Data;
using System.IO;
using System.Net;
using System.Net.WebSockets;

static class Program {
	static Program() {
		Database = new();
		Clients = new();
		Listener = new();
	}

	static async Task Main( string[] args ) {
		Console.WriteLine( "ConfEdit Server V0.1.0" );
		await RunDatabaseUpgrade();

		Listener.Prefixes.Add( "http://localhost:42069/" );
		Listener.Start();

		while ( true ) {
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

	static async Task RunDatabaseUpgrade() {
		Console.WriteLine( "Searching for database upgrades.." );
		string [] stdfiles = Directory.GetFiles( "sql", "std_*.sql" );
		for ( int i = 0; i < stdfiles.Length; ++i ) {
			Console.WriteLine( $"Upgrade found: {stdfiles[i]}" );
			try {
				Database.Execute( File.ReadAllText( stdfiles[i] ) );
			} catch (Exception e) {
				Console.WriteLine( $"Something went wrong executing upgrade {stdfiles[i]}:" );
				Console.WriteLine( e.Message );
			}
		}
		Console.WriteLine( $"Finished! Applied {stdfiles.Length} upgrades" );
	}

	public static Database Database { get; private set; }
	public static ConcurrentDictionary<Guid, CEClient> Clients { get; private set; }
	public static HttpListener Listener { get; private set; }
}
