namespace QuotesCheck
{
    using System;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class DateJsonConverter : JsonConverter<DateTime>
    {
        public override bool CanRead => false;

        public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
        {
            JToken.FromObject(value.ToString("yyyy-MM-dd")).WriteTo(writer);
        }

        public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}