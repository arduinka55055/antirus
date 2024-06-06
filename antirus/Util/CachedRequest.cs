using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Security.Cryptography;
//cache that stores requests in files
//if file exists, then return it

namespace antirus.Util
{
    public class CachedRequest
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string cacheDir = "cache";
        private static readonly string cacheExt = ".json";

        public static async Task<string> Get(string url)
        {
            string hash = BitConverter.ToString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(url))).Replace("/", "");
            string cachePath = cacheDir + "/" + hash + cacheExt;
            if (System.IO.File.Exists(cachePath))
            {
                return System.IO.File.ReadAllText(cachePath);
            }
            else
            {
                try
                {
                    string response = await client.GetStringAsync(url);
                    //make sure cache dir exists
                    System.IO.Directory.CreateDirectory(cacheDir);
                    System.IO.File.WriteAllText(cachePath, response);
                    return response;

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return "";
                }
            }
        }
        public static void ClearCache()
        {
            Directory.Delete(cacheDir, false);
            Directory.CreateDirectory(cacheDir);
        }
    }
}
