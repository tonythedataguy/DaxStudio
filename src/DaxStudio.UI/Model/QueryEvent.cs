﻿using DaxStudio.UI.Enums;
using System;



namespace DaxStudio.UI.Models
{
    public class QueryEvent
    {
        public long Duration { get; set; }
        public string Query { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Username { get; set; }
        public string DatabaseName { get; internal set; }
        public string QueryType { get; set; }
    }
}