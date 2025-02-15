using System.Collections.Concurrent;
using Login.Models;

namespace Login.Services
{
    public static class LiveDataStore
    {
        public static ConcurrentDictionary<int, polRoomCardModel> LivePolRoomData
            = new ConcurrentDictionary<int, polRoomCardModel>();
    }
}
