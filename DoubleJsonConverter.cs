namespace QuotesCheck
{
    using System;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class DoubleJsonConverter : JsonConverter<double>
    {
        public override bool CanRead => false;

        public override void WriteJson(JsonWriter writer, double value, JsonSerializer serializer)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                JToken.FromObject(value).WriteTo(writer);
            }
            else
            {
                JToken.FromObject(decimal.Round(Convert.ToDecimal(value), 2)).WriteTo(writer);
            }
        }

        public override double ReadJson(JsonReader reader, Type objectType, double existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}