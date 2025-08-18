using System.Net.WebSockets;
using System.Text;

public class CEClient {
	public CEClient( Guid id, WebSocket socket ) {
		ID = id;
		mySocket = socket;
	}

	public async Task Handle() {
		byte[] buffer = new byte[1024 * 4]; // 4KB buffer

        try
        {
            while (mySocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await mySocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Received from {ID}: {message}");

                    string response = $"Echo: {message}";
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    await mySocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine($"Client {ID} disconnected.");
                    break;
                }
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

	public Guid ID { get; private set; }
	
	private WebSocket mySocket;
}