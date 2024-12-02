using DAMmodels;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DAM")));
// No need for additional services like controllers or swagger
List<WebSocket> _clients = [];
var app = builder.Build();
Console.WriteLine("Starting WebSocket server...");
// Enable WebSockets middleware
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(120), // Interval to keep the connection alive
    AllowedOrigins = { "*" } // Allow all origins, adjust as necessary
});

// WebSocket handling middleware
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")  // WebSocket endpoint
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            
            // Accept multiple subprotocols by specifying a list of supported protocols
            string acceptedSubprotocol = context.Request.Headers.SecWebSocketProtocol!;

            // List of subprotocols the server accepts
            string[] supportedSubprotocols = Enum.GetValues(typeof(Subprotocol)).Cast<Subprotocol>().Select(subprotocol => subprotocol.ToString()).ToArray();

            // Check if the client requested any subprotocols
            foreach (var protocol in supportedSubprotocols) if (supportedSubprotocols.Contains(acceptedSubprotocol)) break;

            if (string.IsNullOrEmpty(acceptedSubprotocol))
            {
                // If no acceptable subprotocol found, reject the connection
                context.Response.StatusCode = 400;
                return;
            }
            var webSocket = await context.WebSockets.AcceptWebSocketAsync(acceptedSubprotocol);
            Console.WriteLine($"{acceptedSubprotocol} connection established.");
            // Add the new WebSocket to the list
            _clients.Add(webSocket);
            // Resolve the DbContext instance from the service provider
            var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
            await HandleWebSocketAsync(webSocket, dbContext);  // Handle WebSocket communication
        }
        else
        {
            context.Response.StatusCode = 400;  // Bad Request if not a WebSocket request
            Console.WriteLine("Invalid WebSocket request.");
        }
    }
    else await next();
});
Console.WriteLine("WebSocket server is running.");
// Run the application
app.Run();

// WebSocket message handling function
async Task HandleWebSocketAsync(WebSocket webSocket, ApplicationDbContext _DBcontext)
{
    var buffer = new byte[1024 * 4];
    WebSocketReceiveResult result;

    // Continuously receive messages from the WebSocket
    do
    {
        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        if (result.MessageType == WebSocketMessageType.Text)
        {
            var stringMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Message message = JsonSerializer.Deserialize<Message>(stringMessage)!;
            var baseClient = _clients.FirstOrDefault(client => client.SubProtocol == Subprotocol.Base.ToString())!;
            var commandClient = _clients.FirstOrDefault(client => client.SubProtocol == Subprotocol.CommandCenter.ToString())!;
            switch (message.MessageType)
            {
                case MessageType.Registration:
                    Soldier soldier = JsonSerializer.Deserialize<Soldier>(message.Data);
                    _DBcontext.Add(soldier);
                    int r = _DBcontext.SaveChanges();  // Save changes to database

                    // Check if at least 1 row was affected
                    if (r > 0)
                    {
                        soldier.Status = SoldierStatus.Registered;
                        Console.WriteLine($"Soldier {soldier.Id}: {soldier.Name} inserted successfully!");
                        string jsonString = JsonSerializer.Serialize(new Message
                        {
                            MessageType = MessageType.Registered,
                            Data = soldier
                        });
                        // Echo the message back to the client
                        // here it's fine to sent to both clients
                        BroadcastMessage(jsonString);
                    }
                    else
                    {
                        Console.WriteLine("No rows were affected.");
                    }
                    break;
                case MessageType.Deployment:
                    await baseClient.SendAsync(Encoding.UTF8.GetBytes(stringMessage), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                    break;
                case MessageType.Moving:
                    await commandClient.SendAsync(Encoding.UTF8.GetBytes(stringMessage), WebSocketMessageType.Text, result.EndOfMessage, CancellationToken.None);
                    break;
                default:
                    break;
            }
        }

    } while (!result.CloseStatus.HasValue);
    // Log when the WebSocket connection is closed
    Console.WriteLine("WebSocket connection closed.");  
    // Close the WebSocket connection after the loop ends
    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
}

async Task BroadcastMessage(string jsonString)
{
    var buffer = Encoding.UTF8.GetBytes(jsonString);
    var segment = new ArraySegment<byte>(buffer);

    // Create a list of tasks to send the message to each WebSocket connection
    var tasks = _clients.Select(client => client.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None)).ToArray();

    // Wait for all the tasks to finish (i.e., wait until all clients receive the message)
    await Task.WhenAll(tasks);
}

// Remove disconnected WebSockets
async Task RemoveDisconnectedClients()
{
    var disconnectedClients = new List<WebSocket>();

    foreach (var client in _clients)
    {
        if (client.State != WebSocketState.Open)
        {
            disconnectedClients.Add(client);
        }
    }

    // Remove disconnected clients from the list
    foreach (var client in disconnectedClients)
    {
        _clients.Remove(client);
        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
    }
}