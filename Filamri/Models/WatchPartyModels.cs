using System;
using System.Collections.Generic;

namespace filamri.Models
{
    public class WatchRoom
    {
        public string RoomId { get; set; } = "";
        public string HostId { get; set; } = "";
        public string HostName { get; set; } = "";
        public string GuestId { get; set; } = "";
        public string GuestName { get; set; } = "";
        public string VideoUrl { get; set; } = "";  // Вместо VideoPath
        public double CurrentPosition { get; set; } = 0;
        public bool IsPlaying { get; set; } = false;
        public string Status { get; set; } = "waiting";
        public List<ChatMessage> Messages { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class ChatMessage
    {
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Text { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Time => Timestamp.ToString("HH:mm");
    }
}