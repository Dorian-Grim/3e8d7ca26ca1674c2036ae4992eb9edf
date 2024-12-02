using DAMmodels;
using System.Collections.ObjectModel;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CommandCenter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ClientWebSocket _webSocket;
        private ObservableCollection<Soldier> Soldiers { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            _webSocket = new ClientWebSocket();
            _webSocket.Options.AddSubProtocol(Subprotocol.CommandCenter.ToString());
            Soldiers = [];
            SoldiersDataGrid.ItemsSource = Soldiers;  // Bind DataGrid to the collection
            // Create a TextBoxWriter that writes to the ConsoleOutputTextBox
            TextBoxWriter textBoxWriter = new(ConsoleOutputTextBox);
            // Redirect Console output to the TextBoxWriter
            Console.SetOut(textBoxWriter);
            InitMap();
        }
        private async void InitMap()
        {
            Console.WriteLine("Initializing map");
            // Ensure WebView2 is ready
            await MapWebView.EnsureCoreWebView2Async(null);

            // Load local HTML file into WebView2
            MapWebView.CoreWebView2.Navigate(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Map.html"));

            // Wait for navigation to complete and execute additional logic if needed
            MapWebView.CoreWebView2.NavigationCompleted += async (sender, e) =>
            {
                // Your additional logic (e.g., PlaceMarkerAsync) can go here if needed
                Console.WriteLine("Initialized map");
            };
        }
        // Handle the button click event to connect to the WebSocket server
        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            // Change the status to "Connecting..."
            Console.WriteLine("Connecting...");
            
            // Attempt to connect to the WebSocket server
            await ConnectToWebSocketAsync();
        }
        private async void DeployButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the clicked button
            Button button = (Button)sender;

            // Get the DataGridRow associated with the button
            ContentPresenter row = (ContentPresenter)VisualTreeHelper.GetParent(button);

            // Get the DataContext of the row, which is the Soldier object
            Soldier soldier = (Soldier)row.Content;
            
            if (soldier.Status == SoldierStatus.Deployed || soldier.Status == SoldierStatus.Moving) 
            {
                Console.WriteLine("Soldier already deployed...");
                return; 
            }
            InitMarker(soldier.Id, soldier.Location.Lat, soldier.Location.Long);
            // Perform the deploy action with the Soldier's Id
            Console.WriteLine($"Deploying Soldier with Id: {soldier.Id}");
            string jsonString = JsonSerializer.Serialize(new Message
            {

                MessageType = MessageType.Deployment,
                Data = soldier
            });
            byte[] buffer = Encoding.UTF8.GetBytes(jsonString);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        // Async method to connect to the WebSocket server
        private async Task ConnectToWebSocketAsync()
        {
            try
            {
                // Connect to the WebSocket server (adjust URL as needed)
                await _webSocket.ConnectAsync(new Uri("wss://localhost:5001/ws"), CancellationToken.None);
                
                // Update UI on successful connection (ensure thread safety)
                Console.WriteLine("Connected");

                // Start receiving messages from the server
                ReceiveMessagesAsync();
            }
            catch (Exception ex)
            {
                // Handle connection errors (show error message)
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        // Method to receive messages from the WebSocket server
        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[1024];
            try
            {
                // Loop to continuously listen for incoming messages
                while (_webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        // Decode received message and update UI
                        string stringMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Message message = JsonSerializer.Deserialize<Message>(stringMessage)!;
                        Soldier soldier = JsonSerializer.Deserialize<Soldier>(message.Data);
                        switch (message.MessageType)
                        {
                            case MessageType.Registered:
                                Console.WriteLine($"Soldier {soldier!.Name} registered");
                                soldier.Location ??= new Location();
                                Soldiers.Add(soldier!);
                                break;
                            case MessageType.Registration:
                                break;
                            case MessageType.Deployment:
                                Soldiers.FirstOrDefault(s => s.Id == soldier.Id).Status = SoldierStatus.Deployed;
                                SoldiersDataGrid.Items.Refresh();
                                break;
                            case MessageType.Moving:
                                Soldiers.FirstOrDefault(s => s.Id == soldier.Id).Status = SoldierStatus.Moving;
                                SoldiersDataGrid.Items.Refresh();
                                MoveMarkerAsync(soldier.Id, soldier.Location.Lat, soldier.Location.Long);
                                // what happens here is that the grid is refreshed every 1ms or the time it takes for soldiers to move. Set on soldier class at the moment
                                // so if the refresh time is too low. You can't click deploy button
                                // technically the refresh can be as low as 1ms
                                Dispatcher.Invoke(() =>
                                {
                                    // Assuming you have a reference to the DataGrid
                                    Console.WriteLine($"{soldier.Name} at Latitude {soldier.Location.Lat}, Longitude {soldier.Location.Long}");
                                });
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving message: {ex.Message}");
            }
        }

        // Method to move the marker to a new position
        private async Task InitMarker(int SoldierId, double lat, double lon)
        {
            string script = $@"initMarker({SoldierId}, {lat}, {lon});";

            await MapWebView.CoreWebView2.ExecuteScriptAsync(script);
        }
        private async Task MoveMarkerAsync(int SoldierId, double lat, double lon)
        {
            string script = $@"updateMarkerPosition({SoldierId}, {lat}, {lon});";

            await MapWebView.CoreWebView2.ExecuteScriptAsync(script);
        }
    }
}