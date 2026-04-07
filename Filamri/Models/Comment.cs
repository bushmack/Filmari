namespace filamri.Models
{
    public class Comment
    {
        public int MovieId { get; set; }
        public string UserName { get; set; } = "";
        public string Text { get; set; } = "";
        public string Timestamp { get; set; } = "";
        public string UserId { get; set; } = "";
    }
}