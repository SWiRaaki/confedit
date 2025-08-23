using System.Collections.Concurrent;
using System.Data;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

internal class AppConfig {
	[JsonProperty("host")]
	internal string Host { get; set; } = "";

	[JsonProperty("port")]
	internal ushort Port { get; set; } = 0;

	[JsonProperty("jwts")]
	internal string Secret { get; set; } = "";
}

internal static class Program {
	static Program() {
		Database = new();
		Clients = new();
		Listener = new();
		Module = new();
		Config = new();
		Module auth = new ModuleAuth();
		Module.Add( auth.Name, auth );
	}

	static async Task Main( string[] args ) {
		Console.WriteLine( "ConfEdit Server V0.2.0" );
		Console.WriteLine( "Reading configurations.." );
		using (StreamReader stream = new( File.Open( "confedit.json" , FileMode.Open, FileAccess.Read ) ) ) {
			Config = JsonConvert.DeserializeObject<AppConfig>( stream.ReadToEnd() ) ?? new();
		}
		//await RunDatabaseUpgrade();

		Console.WriteLine( "Starting up listener server..");
		Listener.Prefixes.Add( $"http://{Config.Host}:{Config.Port}/" );
		Listener.Start();

		Console.WriteLine( $"Listening on {Config.Host}:{Config.Port}.." );
		while ( true ) {
			var context = await Listener.GetContextAsync();
			if (context.Request.IsWebSocketRequest)
			{
				var wsContext = await context.AcceptWebSocketAsync( null );
				WebSocket webSocket = wsContext.WebSocket;

				Client client = new Client( webSocket );
				if ( await client.Handshake() ) {
					Clients.TryAdd( client.ID, client );
					Console.WriteLine( $"Client connected: {client.ID} (Total: {Clients.Count})" );

					_ = client.Handle();
				}
			}
			else
			{
				context.Response.StatusCode = 400;
				context.Response.Close();
			}
		}
	}

	static void RunDatabaseUpgrade() {
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

	internal static Database Database { get; private set; }
	internal static ConcurrentDictionary<Guid, Client> Clients { get; private set; }
	internal static HttpListener Listener { get; private set; }
	internal static Dictionary<string, Module> Module { get; private set; }
	internal static AppConfig Config { get; private set; }
}
