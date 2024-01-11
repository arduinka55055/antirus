using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace antirus.Tests
{
    [TestClass]
    public class Tester
    {
        [TestInitialize]
        public void Setup()
        {
           IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
           ILogger<Player> logger = new LoggerFactory().CreateLogger<Player>();
           Player.Init(logger, cache);
        }
        [TestMethod]
        public void Bulk()
        {
            Player player = new("gameplayer55055");
            player.LoadPlayer().Wait();
            player.LoadGames().Wait();

            player.LoadFriends().Wait();
            //load friends of friends
            var tasks = new List<System.Threading.Tasks.Task>();
            foreach (var friend in player.Friends)
            {
                tasks.Add(friend.LoadFriends());
            }
            System.Threading.Tasks.Task.WaitAll(tasks.ToArray());
            Console.WriteLine(player);
            //save all data as json
            string json = System.Text.Json.JsonSerializer.Serialize(player);
            System.IO.File.WriteAllText("player.json", json);
            
            Assert.IsTrue(player.IsRussian<0);
        }
        [TestMethod]
        public void TestLoadPlayer()
        {
            Player player = new("l5cker");
            player.LoadPlayer().Wait();
            Assert.IsTrue(player.Name.Length>0);
            Console.WriteLine(player);
            Console.WriteLine(JsonSerializer.Serialize(player));
        }
        [TestMethod]
        public void TestLoadGames()
        {
            Player player = Player.Get("76561198846167661");
            player.LoadGames().Wait();
            Assert.IsTrue(player.Games.Count>0);
            Console.WriteLine(player);
        }
        [TestMethod]
        public void TestLoadFriends()
        {
            Player player = Player.Get("76561198846167661");
            player.LoadFriends().Wait();
            Assert.IsTrue(player.Friends.Count>0);
            Console.WriteLine(player);
        }
        [TestMethod]
        public void TestCaching()
        {
            Player player = Player.Get("76561198846167661");
            Player player2 = Player.Get("76561198846167661");
            Assert.IsTrue(player==player2);
            
            //load games of #1 player
            player.LoadGames().Wait();

            //check if #2 player has games loaded
            Assert.IsTrue(player2.Games.Count>0);

            Console.WriteLine(player);
        }
    }
}