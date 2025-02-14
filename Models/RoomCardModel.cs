using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Login.Models
{
    public class RoomCardModel
    {
        #nullable disable
        public int id { get; set; }
        public string RoomName { get; set; }
        public string Status { get; set; }
        public int Temperature { get; set; }
        public bool IsLocked { get; set; }
    }
}