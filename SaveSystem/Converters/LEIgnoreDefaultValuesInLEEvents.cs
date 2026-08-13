using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FS_LevelEditor.SaveSystem.Converters
{
    public class LEIgnoreDefaultValuesInLEEvents : JsonConverter<LE_Event>
    {
        public override LE_Event ReadJson(JsonReader reader, Type objectType, LE_Event existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            Logger.Error("[SAVE FILE] LEIgnoreDefaultValuesInLEEvents converter is for write only.");
            throw new NotSupportedException("[SAVE FILE] LEIgnoreDefaultValuesInLEEvents converter is for write only.");
        }

        public override void WriteJson(JsonWriter writer, LE_Event value, JsonSerializer serializer)
        {
            var defaultInstance = new LE_Event();
            writer.WriteStartObject();

            foreach (var property in typeof(LE_Event).GetProperties())
            {
                if (!property.CanRead || !property.CanWrite) continue;
                object defaultValue = property.GetValue(defaultInstance);
                object currentValue = property.GetValue(value);

                if (!CustomEquals(defaultValue, currentValue))
                {
                    writer.WritePropertyName(property.Name);
                    serializer.Serialize(writer, currentValue, property.PropertyType);
                }
            }

            writer.WriteEndObject();
        }

        public static bool CustomEquals(object value1, object value2)
        {
            if (value1 is IList list1 && value2 is IList list2)
            {
                if (list1.Count == 0 && list2.Count == 0)
                    return true;
            }

            return Equals(value1, value2);
        }
    }
}