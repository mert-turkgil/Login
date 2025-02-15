using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Login.Identity;

namespace Login.Models
{
    public class UserViewModel
    {
        #nullable disable
        public string Id { get; set; }  
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string UserName { get; set; }
        public User User { get; set; }

        public DateTime CreatedDate { get; set; }

        public List<polRoomCardModel> RoomCards { get; set; }= new List<polRoomCardModel>();
    }
}