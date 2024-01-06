namespace antirus.Tests
{
    [TestClass]
    public class Tester
    {
        [TestMethod]
        public void Testplayer()
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
            
            Assert.IsTrue(player.IsRussian(false)<0);
        }
    }
}