using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FS_LevelEditor.SaveSystem.Converters
{
    public class OldPropertiesRename<T> : JsonConverter<T>
    {
        private readonly Dictionary<string, string> renames;
        private readonly Dictionary<string, Func<JsonElement, object>> valueConverters;

        // Cached once per converter instance instead of rebuilt on every Read() call.
        private JsonSerializerOptions cachedFallbackOptions;

        public OldPropertiesRename(Dictionary<string, string> renames, Dictionary<string, Func<JsonElement, object>> valueConverters = null)
        {
            this.renames = renames;
            this.valueConverters = valueConverters ?? new Dictionary<string, Func<JsonElement, object>>();
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                var root = doc.RootElement;

                // Fast path: if none of the properties on this object need renaming or
                // value-converting, skip the full rewrite round-trip and deserialize directly
                // using the (cached) fallback options.
                bool needsRewrite = false;
                foreach (var prop in root.EnumerateObject())
                {
                    if (renames.ContainsKey(prop.Name) || valueConverters.ContainsKey(
                            renames.ContainsKey(prop.Name) ? renames[prop.Name] : prop.Name))
                    {
                        needsRewrite = true;
                        break;
                    }
                }

                var fallbackOptions = GetFallbackOptions(options);

                if (!needsRewrite)
                {
                    return JsonSerializer.Deserialize<T>(root.GetRawText(), fallbackOptions);
                }

                using (MemoryStream modifiedStream = new MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(modifiedStream))
                    {
                        writer.WriteStartObject();

                        foreach (var prop in root.EnumerateObject())
                        {
                            // Check for the final name (rename if it exists on the "renames" dict).
                            string targetName = renames.ContainsKey(prop.Name) ? renames[prop.Name] : prop.Name;
                            writer.WritePropertyName(targetName);

                            // Check if this name has a value conversion func.
                            if (valueConverters.TryGetValue(targetName, out var converter))
                            {
                                var convertedValue = converter(prop.Value);
                                // Serialize the converted value.
                                JsonSerializer.Serialize(writer, convertedValue, fallbackOptions);
                            }
                            else
                            {
                                // Serialie the original value, no modifications at all.
                                prop.Value.WriteTo(writer);
                            }
                        }

                        writer.WriteEndObject();
                    }

                    modifiedStream.Position = 0;

                    return JsonSerializer.Deserialize<T>(modifiedStream, fallbackOptions);
                }
            }
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            Logger.Error("[SAVE FILE] OldPropertiesRename converter is for read only.");
            throw new NotSupportedException("[SAVE FILE] OldPropertiesRename converter is for read only.");
        }

        // Builds (once) and caches the options clone with this converter removed, to avoid
        // infinite recursion. Previously this was rebuilt via a fresh JsonSerializerOptions
        // clone + LINQ scan on every single Read() call.
        private JsonSerializerOptions GetFallbackOptions(JsonSerializerOptions options)
        {
            if (cachedFallbackOptions != null) return cachedFallbackOptions;

            var fallback = new JsonSerializerOptions(options);
            var existing = fallback.Converters.FirstOrDefault(c => c is OldPropertiesRename<T>);
            if (existing != null) fallback.Converters.Remove(existing);

            cachedFallbackOptions = fallback;
            return cachedFallbackOptions;
        }
    }
}