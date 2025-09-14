using System.Collections.Concurrent;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

internal class AppConfig
{
    [JsonProperty("host")]
    internal string Host { get; set; } = "";

    [JsonProperty("port")]
    internal ushort Port { get; set; } = 0;

    [JsonProperty("jwts")]
    internal string Secret { get; set; } = "";
}

internal static class Program
{
    static Program()
    {
        Database = new();
        Clients = new();
        Listener = new();
        Module = new();
        Config = new();
        Scripter = new();
        ConfigProvider = new();

        var auth = new ModuleAuth();
        Module.Add(auth.Name, auth);

        var admin = new ModuleAdmin();
        Module.Add(admin.Name, admin);

        var fm = new ModuleFm();
        Module.Add(fm.Name, fm);

		var sriptql = new ScriptQLite();
		Scripter.Add( sriptql.Name, sriptql );
		Script = sriptql;

        var xmlprovider = new XmlConfigProvider();
        ConfigProvider.Add(".xml", xmlprovider);

        var jsonprovider = new JsonConfigProvider();
        ConfigProvider.Add(".json", jsonprovider);

        var yamlprovider = new YamlConfigProvider();
        ConfigProvider.Add(".yaml", yamlprovider);

        var tomlprovider = new TomlConfigProvider();
        ConfigProvider.Add(".toml", tomlprovider);
    }

    static async Task Main(string[] args)
    {
        Console.WriteLine("ConfEdit Server V1.0.0");
        Console.WriteLine("Reading configurations..");
        using (StreamReader stream = new(File.Open("confedit.json", FileMode.Open, FileAccess.Read)))
        {
            Config = JsonConvert.DeserializeObject<AppConfig>(stream.ReadToEnd()) ?? new();
        }

        Console.WriteLine("Searching for database upgrades..");
        Script.CheckForPatches();

        Console.WriteLine("Starting up listener server..");
        Listener.Prefixes.Add($"http://{Config.Host}:{Config.Port}/");
        Listener.Start();

		Console.WriteLine( $"Listening on {Config.Host}:{Config.Port}.." );
		while ( true ) {
			var context = await Listener.GetContextAsync();
			if ( context.Request.IsWebSocketRequest )
			{
				var wsContext = await context.AcceptWebSocketAsync( null );
				WebSocket webSocket = wsContext.WebSocket;

				Client client = new Client( webSocket );
				if ( await client.Handshake() ) {
					Clients.TryAdd( client.ID, client );
					Console.WriteLine( $"Client connected: {client.ID} (Total: {Clients.Count})" );

					_ = client.Handle();
				} else {
                    await webSocket.CloseAsync( WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None );
                }
            }
			else if ( context.Request.HttpMethod == "GET" ) {
				var rsx = context.Request.Url!.AbsolutePath;
				if( rsx == "/" ) {
					Console.WriteLine( "App request: Redirect to web frontend.." );
					rsx = "/login.html";
				}
				var path = Path.Combine( Environment.CurrentDirectory, "app", rsx.Remove( 0, 1 ) );
				Console.WriteLine( path );

				if ( !File.Exists( path ) ) {
					context.Response.StatusCode = 404;
					context.Response.Close();
					continue;
				}

				var ext = Path.GetExtension( path );
				var binary = false;
				switch( ext ) {
				case ".html":
					context.Response.ContentType = "text/html";
					break;
				case ".js":
					context.Response.ContentType = "text/javascript";
					break;
				case ".css":
					context.Response.ContentType = "text/css";
					break;
				case ".ico":
					context.Response.ContentType = "image/vnd.microsoft.icon";
					binary = true;
					break;
				default:
					context.Response.ContentType = "text/plain";
					break;
				}

				byte[] data;
				if ( binary ) {
					data = File.ReadAllBytes( path );
				}
				else {
					data = Encoding.UTF8.GetBytes( File.ReadAllText( path ) );
					context.Response.ContentEncoding = Encoding.UTF8;
				}
				context.Response.ContentLength64 = data.LongLength;

				await context.Response.OutputStream.WriteAsync( data );
				context.Response.Close();
			}
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

	static byte[] Compress(byte[] data)
	{
		using (var compressedStream = new MemoryStream())
		using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
		{
			zipStream.Write(data, 0, data.Length);
			zipStream.Close();
			return compressedStream.ToArray();
		}
	}

	static byte[] Decompress(byte[] data)
	{
		using (var compressedStream = new MemoryStream(data))
		using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
		using (var resultStream = new MemoryStream())
		{
			zipStream.CopyTo(resultStream);
			return resultStream.ToArray();
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

    internal static void PrintTable(DataTable data)
    {
        Console.WriteLine();
        Dictionary<string, int> colWidths = new Dictionary<string, int>();

        foreach (DataColumn col in data.Columns)
        {
            Console.Write(col.ColumnName);
            var maxLabelSize = data.Rows.OfType<DataRow>()
                    .Select(m => (m.Field<object>(col.ColumnName)?.ToString() ?? "").Length)
                    .OrderByDescending(m => m).FirstOrDefault();

            colWidths.Add(col.ColumnName, maxLabelSize);
            for (int i = 0; i < maxLabelSize - col.ColumnName.Length + 10; ++i) Console.Write(" ");
        }

        Console.WriteLine();

        foreach (DataRow dataRow in data.Rows)
        {
            for (int j = 0; j < dataRow.ItemArray.Length; ++j)
            {
                Console.Write(dataRow.ItemArray[j]);
                for (int i = 0; i < colWidths[data.Columns[j].ColumnName] - dataRow.ItemArray[j]!.ToString()!.Length + 10; ++i)
					Console.Write(" ");
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
	internal static Script Script { get; private set; }
	internal static Dictionary<string, IConfigProvider> ConfigProvider { get; private set; }
}
