using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Login.Services
{
    public class MqttConfig
    {
        #nullable disable
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ClientId { get; set; } = "mvc-website";
    }
}