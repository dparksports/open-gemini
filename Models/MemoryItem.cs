using System;

namespace OpenClaw.Windows.Models
{
    public class MemoryItem
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public float[] Vector { get; set; } = Array.Empty<float>();
        public DateTime Timestamp { get; set; }
    }
}
