namespace DAMmodels
{
    public class Message
    {
        public required MessageType MessageType { get; set; }
        public required dynamic Data { get; set; }
    }
}
