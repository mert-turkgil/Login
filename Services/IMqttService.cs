using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Login.Services
{
    public interface IMqttService
    {
        Task ConnectAsync();
        Task PublishAsync(string topic, string message);
    }
}