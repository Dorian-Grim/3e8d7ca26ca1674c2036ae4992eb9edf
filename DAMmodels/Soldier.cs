using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.NetworkInformation;

namespace DAMmodels
{
    public class Soldier
    {
        public int Id { get; set; }
        [NotMapped] public SoldierStatus Status { get; set; } = SoldierStatus.Unregistered;
        public required string Name { get; set; }
        public required string Rank { get; set; }
        public string? Country { get; set; }
        public string? TrainingInfo { get; set; }
        [NotMapped]public Location ?Location { get; set; }

        public static async Task MoveSoldierAsync(Soldier soldier, double speedMetersPerSecond, int durationSeconds, Action<Soldier> onProgress, CancellationToken cancellationToken)
        {
            // Move the soldier every second
            double distancePerSecond = speedMetersPerSecond;
            double totalDistance = distancePerSecond * durationSeconds;

            Console.WriteLine($"Starting movement for {soldier.Name}...");

            for (int second = 0; second < durationSeconds; second++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine($"Movement for {soldier.Name} was cancelled.");
                    break;
                }
                // Move the soldier every second
                soldier.Location!.Move(distancePerSecond, 0); // Moving in the "east" direction (longitude).
                 // Invoke callback to send progress
                onProgress?.Invoke(soldier);
                Console.WriteLine($"{soldier.Name} at Latitude {soldier.Location.Lat}, Longitude {soldier.Location.Long}");

                // Wait for the next second
                await Task.Delay(1000, cancellationToken); // 1 second delay
            }

            Console.WriteLine($"Movement complete for {soldier.Name}. Final position: Latitude {soldier.Location!.Lat}, Longitude {soldier.Location.Long}");
        }
    }
}
