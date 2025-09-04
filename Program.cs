using System.Collections.Concurrent;
using System.Data;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
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
		Scripter = new();

		var auth = new ModuleAuth();
		Module.Add( auth.Name, auth );

		var admin = new ModuleAdmin();
		Module.Add( admin.Name, admin );

		var sriptql = new ScriptQLite();
		Scripter.Add( sriptql.Name, sriptql );
	}

	static async Task Main( string[] args ) {
		Console.WriteLine( "ConfEdit Server V0.2.0" );
		Console.WriteLine( "Reading configurations.." );
		using (StreamReader stream = new( File.Open( "confedit.json" , FileMode.Open, FileAccess.Read ) ) ) {
			Config = JsonConvert.DeserializeObject<AppConfig>( stream.ReadToEnd() ) ?? new();
		}

		Console.WriteLine( "Searching for database upgrades.." );
		Script.CheckForPatches();

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
				if (await client.Handshake()) {
					Clients.TryAdd(client.ID, client);
					Console.WriteLine($"Client connected: {client.ID} (Total: {Clients.Count})");

					_ = client.Handle();
				} else {
					var buf = Encoding.UTF8.GetBytes("Authentification failed");
					await webSocket.SendAsync(new ArraySegment<byte>(buf), WebSocketMessageType.Text, true, CancellationToken.None);
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
			}
			else
			{
				context.Response.StatusCode = 400;
				context.Response.Close();
			}
		}
	}

	internal static void WriteToCsv(this DataTable table, string filePath, bool includeHeaders = true)
    {
        using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
        {
            if (includeHeaders)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    writer.Write(table.Columns[i].ColumnName);
                    if (i < table.Columns.Count - 1)
                        writer.Write(",");
                }
                writer.WriteLine();
            }

            foreach (DataRow row in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    var value = row[i]?.ToString()?.Replace("\"", "\"\"") ?? string.Empty;
                    // Surround with quotes if value contains commas or quotes
                    if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                    {
                        value = $"\"{value}\"";
                    }
                    writer.Write(value);

                    if (i < table.Columns.Count - 1)
                        writer.Write(",");
                }
                writer.WriteLine();
            }
        }
    }

	internal static void PrintTable( DataTable data ) {
		Console.WriteLine();
        Dictionary<string, int> colWidths = new Dictionary<string, int>();

        foreach ( DataColumn col in data.Columns )
        {
            Console.Write( col.ColumnName );
            var maxLabelSize = data.Rows.OfType<DataRow>()
                    .Select( m => ( m.Field<object>( col.ColumnName )?.ToString() ?? "" ).Length )
                    .OrderByDescending( m => m ).FirstOrDefault();

            colWidths.Add( col.ColumnName, maxLabelSize );
            for ( int i = 0; i < maxLabelSize - col.ColumnName.Length + 10; ++i ) Console.Write( " " );
        }

        Console.WriteLine();

        foreach ( DataRow dataRow in data.Rows )
        {
            for ( int j = 0; j < dataRow.ItemArray.Length; ++j )
            {
                Console.Write( dataRow.ItemArray[j] );
                for ( int i = 0; i < colWidths[data.Columns[j].ColumnName] - dataRow.ItemArray[j]!.ToString()!.Length + 10; ++i ) Console.Write( " " );
            }
            Console.WriteLine();
        }
	}

	internal static Database Database { get; private set; }
	internal static ConcurrentDictionary<Guid, Client> Clients { get; private set; }
	internal static HttpListener Listener { get; private set; }
	internal static Dictionary<string, Module> Module { get; private set; }
	internal static AppConfig Config { get; private set; }
	internal static Dictionary<string, Script> Scripter { get; private set; }
}
