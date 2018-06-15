namespace QuotesCheck
{
    using System.Diagnostics;
    using System.IO;

    using Newtonsoft.Json;

    internal static class Json
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };

        public static void Save(string dataPath, object obj)
        {
            Directory.CreateDirectory(new FileInfo(dataPath).DirectoryName);
            File.WriteAllText(dataPath, JsonConvert.SerializeObject(obj, JsonSettings));
        }

        public static T Load<T>(string dataPath)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(dataPath), JsonSettings);
        }
    }
}