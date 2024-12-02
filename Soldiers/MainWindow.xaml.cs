using DAMmodels;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Soldiers
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
            _webSocket.Options.AddSubProtocol(Subprotocol.Base.ToString());
            Soldiers = [];
            SoldiersDataGrid.ItemsSource = Soldiers;  // Bind DataGrid to the collection
            // Create a TextBoxWriter that writes to the ConsoleOutputTextBox
            TextBoxWriter textBoxWriter = new(ConsoleOutputTextBox);
            // Redirect Console output to the TextBoxWriter
            Console.SetOut(textBoxWriter);
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a SoldierRegistration object and map the captured data to its properties
            Soldier registration = new()
            {
                Name = NameTextBox.Text,                   // Map Name
                Rank = RankTextBox.Text,                   // Map Rank
                Country = (CountryComboBox.SelectedItem as ComboBoxItem)?.Content.ToString(), // Map Country
                TrainingInfo = TrainingTextBox.Text       // Map Training Info
            };
            // Optional: Validate that all required fields are filled in
            if (string.IsNullOrEmpty(registration.Name) || string.IsNullOrEmpty(registration.Rank) || string.IsNullOrEmpty(registration.Country))
            {
                Console.WriteLine("Please fill in all fields.");
            }
            else
            {
                connectedResend: if (_webSocket.State == WebSocketState.Open)
                {
                    Console.WriteLine($"Registering {registration.Name}");
                    string jsonString = JsonSerializer.Serialize(new Message
                    {
                        MessageType = MessageType.Registration,
                        Data = registration
                    });

                    // Convert the JSON string to a byte array
                    byte[] buffer = Encoding.UTF8.GetBytes(jsonString);

                    // Send the JSON data as a WebSocket message
                    await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                else 
                {
                    try
                    {
                        await ConnectToWebSocketAsync(); 
                        goto connectedResend; 
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }
            }
        }

        private async Task ConnectToWebSocketAsync()
        {
            // Connect to the WebSocket server (adjust URL as needed)
            await _webSocket.ConnectAsync(new Uri("wss://localhost:5001/ws"), CancellationToken.None);
            // Start receiving messages from the server
            ReceiveMessagesAsync();
        }

        // Method to receive messages from the WebSocket server
        private async Task ReceiveMessagesAsync()
        {
            try
            {
                var buffer = new byte[1024];
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
                                // you will get id here
                                // and registered status
                                Soldiers.Add(soldier!);
                                // can remove input data
                                NameTextBox.Clear();
                                RankTextBox.Clear();
                                CountryComboBox.SelectedIndex = -1;  // Reset the ComboBox selection
                                TrainingTextBox.Clear();
                                break;
                            case MessageType.Registration:
                                break;
                            case MessageType.Deployment:
                                if(soldier.Status == SoldierStatus.Deployed || soldier.Status == SoldierStatus.Moving) return;
                                CancellationTokenSource cts = new();
                                // I am using a callback to broadcast back to clients
                                // I dont need to await this since I can have multiple deployments
                                Soldiers.FirstOrDefault(s => s.Id == soldier.Id).Status = SoldierStatus.Deployed;
                                SoldiersDataGrid.Items.Refresh();
                                Soldier.MoveSoldierAsync(soldier, speedMetersPerSecond: 100, durationSeconds: 60 /*one minute equivalent in Milliseconds*/, onProgress: SendProgressToClients, cancellationToken: cts.Token);
                                break;
                            case MessageType.Moving:
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex )
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        private void SendProgressToClients(Soldier soldier)
        {
            // Simulate sending updates to specific clients
            // Example: Send message to clients about soldier's progress
            soldier.Status = SoldierStatus.Moving;
            Soldiers.FirstOrDefault(s => s.Id == soldier.Id).Status = SoldierStatus.Moving;
            SoldiersDataGrid.Items.Refresh();
            string jsonString = JsonSerializer.Serialize(new Message
            {
                MessageType = MessageType.Moving,
                Data = soldier
            });

            // Send the JSON data as a WebSocket message
            _webSocket.SendAsync(Encoding.UTF8.GetBytes(jsonString), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}