using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAI.Chat;

namespace EngineerEval.Core.LanguageModels;

public static class OpenAiSchemaGenerator
{
    public static ChatResponseFormat Generate(Type type, string messageName)
    {
        var parameters= GenerateObjectSchema(type);
        var schema= JsonSerializer.Serialize(parameters, new JsonSerializerOptions{WriteIndented = true});
        var responseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: messageName,
            jsonSchema: BinaryData.FromString(schema),
            jsonSchemaIsStrict: true);
        return responseFormat;
    }

    private static object GenerateObjectSchema(Type type)
    {
        var props = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                continue;

            var name = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? prop.Name;
            var description = prop.GetCustomAttribute<DescriptionAttribute>()?.Description;

            var propSchema = GenerateSchemaForType(prop.PropertyType);
            if (!string.IsNullOrEmpty(description))
                ((Dictionary<string, object>)propSchema)["description"] = description;

            props[name] = propSchema;
            required.Add(name);
        }

        return new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = props,
            ["required"] = required,
            ["additionalProperties"] = false
        };
    }

    private static object GenerateSchemaForType(Type type)
    {
        if (type == typeof(string)) return new Dictionary<string, object> { ["type"] = "string" };
        if (type == typeof(int) || type == typeof(long)) return new Dictionary<string, object> { ["type"] = "integer" };
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) return new Dictionary<string, object> { ["type"] = "number" };
        if (type == typeof(bool)) return new Dictionary<string, object> { ["type"] = "boolean" };

        if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
        {
            var elementType = type.IsArray
                ? type.GetElementType()
                : type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

            return new Dictionary<string, object>
            {
                ["type"] = "array",
                ["items"] = GenerateSchemaForType(elementType!)
            };
        }

        // Nested object
        return GenerateObjectSchema(type);
    }
}