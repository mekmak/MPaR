using System;
using System.Collections.Generic;

namespace MPR.Models
{
    public class F1Schedule
    {
        public List<Race> Races { get; set; }
    }

    public class Race
    {
        public string Name { get; set; }
        public int Order { get; set; }
        public string Link { get; set; }
        public List<Event> Events { get; set; }
    }

    public class Event
    {
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public string Status { get; set; }
    }
}