using System.Collections;
using System.Reflection;
using Google.Cloud.AIPlatform.V1;
using Type = Google.Cloud.AIPlatform.V1.Type;

namespace EngineerEval.Core.LanguageModels;

public static class OpenApiSchemaGenerator
{
    public static OpenApiSchema Generate(System.Type type)
    {
        return Generate(type, new HashSet<System.Type>());
    }

    private static OpenApiSchema Generate(System.Type type, HashSet<System.Type> seenTypes)
    {
        if (type == typeof(string)) return new OpenApiSchema { Type = Type.String };
        if (type == typeof(int) || type == typeof(long)) return new OpenApiSchema { Type = Type.Integer };
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) return new OpenApiSchema { Type = Type.Number };
        if (type == typeof(bool)) return new OpenApiSchema { Type = Type.Boolean };

        if (type.IsArray || (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type)))
        {
            var elementType = type.IsArray
                ? type.GetElementType()
                : type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

            return new OpenApiSchema
            {
                Type = Type.Array,
                Items = Generate(elementType!, seenTypes)
            };
        }

        if (type.IsClass && type != typeof(string))
        {
            if (!seenTypes.Add(type))
                return new OpenApiSchema { Type = Type.Object }; // Prevent infinite recursion

            var schema = new OpenApiSchema
            {
                Type = Type.Object
            };
            
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>() != null)
                    continue;

                var propName = prop.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>()?.Name ?? prop.Name;
                var propSchema = Generate(prop.PropertyType, seenTypes);

                schema.Properties.Add(propName, propSchema);
                schema.Required.Add(propName); // Assume all properties are required unless you add logic later to mark optional fields
            }

            return schema;
        }

        return new OpenApiSchema { Type = Type.String }; // fallback for unknowns
    }

    private static bool IsNullable(System.Type type)
    {
        if (!type.IsValueType) return true; // ref types are nullable by default
        return Nullable.GetUnderlyingType(type) != null;
    }
}