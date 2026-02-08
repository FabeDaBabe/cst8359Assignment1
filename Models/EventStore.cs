using System.Collections.Generic;

namespace Assignment_1.Models
{
    public static class EventStore
    {
        public static List<Event> Events { get; } = new();
    }
}