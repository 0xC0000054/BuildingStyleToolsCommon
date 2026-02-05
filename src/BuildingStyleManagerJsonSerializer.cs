// Copyright (c) 2026 Nicholas Hayes
// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.Json.Serialization;

namespace BuildingStyleToolsCommon
{
    public sealed class BuildingStyleManagerJsonSerializer : IBuildingStyleManagerSerialization
    {
        private readonly string cacheFilePath;

        private static readonly JsonSerializerOptions SerializerOptions = BuildJsonSerializerOptions();

        public BuildingStyleManagerJsonSerializer(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            cacheFilePath = path;
        }

        public Dictionary<uint, BuildingStyleInfo> Load()
        {
            Dictionary<uint, BuildingStyleInfo> buildingStyles;

            using (FileStream stream = new(cacheFilePath, FileMode.Open, FileAccess.Read))
            {
                var collection = JsonSerializer.Deserialize<Dictionary<uint, BuildingStyleInfo>>(stream, SerializerOptions);

                if (collection is not null)
                {
                    buildingStyles = collection;
                }
                else
                {
                    buildingStyles = [];
                }
            }

            return buildingStyles;
        }

        public void Save(Dictionary<uint, BuildingStyleInfo> buildingStyles)
        {
            using (FileStream stream = new(cacheFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, buildingStyles, SerializerOptions);
            }
        }

        private static JsonSerializerOptions BuildJsonSerializerOptions()
        {
            JsonSerializerOptions options = new();
            options.Converters.Add(new BuildingStyleInfoJsonConverter());

            return options;
        }

        private sealed class BuildingStyleInfoJsonConverter : JsonConverter<BuildingStyleInfo>
        {
            private static ReadOnlySpan<byte> NamePropertyNameUtf8 => "name"u8;
            private static ReadOnlySpan<byte> AuthorPropertyNameUtf8 => "author"u8;
            private static ReadOnlySpan<byte> DescriptionPropertyNameUtf8 => "description"u8;

            public BuildingStyleInfoJsonConverter()
            {
            }

            public override BuildingStyleInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                string name = string.Empty;
                string author = string.Empty;
                string description = string.Empty;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        return new(name, author, description);
                    }

                    // Get the key.
                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        throw new JsonException($"Failed to deserialize the {nameof(BuildingStyleInfo)} object.");
                    }

                    if (reader.ValueTextEquals(NamePropertyNameUtf8))
                    {
                        name = ReadStringValue(ref reader);
                    }
                    else if (reader.ValueTextEquals(AuthorPropertyNameUtf8))
                    {
                        author = ReadStringValue(ref reader);
                    }
                    else if (reader.ValueTextEquals(DescriptionPropertyNameUtf8))
                    {
                        description = ReadStringValue(ref reader);
                    }
                }

                throw new JsonException($"Failed to deserialize the {nameof(BuildingStyleInfo)} object.");
            }

            public override void Write(Utf8JsonWriter writer, BuildingStyleInfo value, JsonSerializerOptions options)
            {
                writer.WriteString(NamePropertyNameUtf8, value.Name);
                writer.WriteString(AuthorPropertyNameUtf8, value.Author);
                writer.WriteString(DescriptionPropertyNameUtf8, value.Description);
            }

            private static string ReadStringValue(ref Utf8JsonReader reader)
            {
                reader.Read();

                if (reader.TokenType != JsonTokenType.String)
                {
                    throw new JsonException($"Failed to deserialize the {nameof(BuildingStyleInfo)} object.");
                }

                return reader.GetString()!;
            }
        }
    }
}
