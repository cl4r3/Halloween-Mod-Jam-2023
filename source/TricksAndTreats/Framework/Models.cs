﻿using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TricksAndTreats
{
    public class Celebrant
    {
        public string[] Roles { get; set; }
        public string[] LovedTreats { get; set; }
        public string[] NeutralTreats { get; set; }
        public string[] HatedTreats { get; set; }
#nullable enable
        public string[]? TreatsToGive { get; set; }
        public string[]? PreferredTricks { get; set; }
        public bool? ReceivedGift = false;
        public bool? GaveGift = false;
#nullable disable
    }

    public class Costume
    {
#nullable enable
        public string? Hat { get; set; }
        public string? Top { get; set; }
        public string? Bottom { get; set; }
        public int? NumPieces;
#nullable disable
    }

    public class Treat
    {
        public bool HalloweenOnly { get; set; }
#nullable enable
        public string? Universal { get; set; }
        public string[]? Flavors { get; set; }
        public int? ObjectId { get; set; }
#nullable disable
    }
}