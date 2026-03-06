using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Menace.Modkit.App.ViewModels;

/// <summary>
/// ViewModel for the EventHandler editor dialog
/// </summary>
public class EventHandlerEditorViewModel
{
    public string FieldName { get; }
    public StatsEditorViewModel ParentVm { get; }
    public ObservableCollection<HandlerItem> Handlers { get; }
    public HandlerItem? SelectedHandler { get; set; }

    public EventHandlerEditorViewModel(
        string fieldName,
        JsonElement arrayElement,
        StatsEditorViewModel parentVm)
    {
        FieldName = fieldName;
        ParentVm = parentVm;
        Handlers = new ObservableCollection<HandlerItem>();

        // Parse existing handlers
        int index = 0;
        foreach (var el in arrayElement.EnumerateArray())
        {
            if (el.ValueKind == JsonValueKind.Object)
            {
                var dict = new Dictionary<string, object?>();
                foreach (var prop in el.EnumerateObject())
                {
                    dict[prop.Name] = CloneJsonValue(prop.Value);
                }

                var typeName = dict.TryGetValue("_type", out var t)
                    ? ExtractStringValue(t)
                    : null;

                Handlers.Add(new HandlerItem
                {
                    Index = index,
                    TypeName = typeName,
                    Data = dict
                });
            }
            index++;
        }
    }

    /// <summary>
    /// Get all EventHandler types from schema
    /// </summary>
    public List<string> GetAllEventHandlerTypes()
    {
        // Get from schema - effect_handlers are stored as embedded classes
        return ParentVm.SchemaService?
            .GetAllEmbeddedClassNames()
            .OrderBy(name => name)
            .ToList() ?? new List<string>();
    }

    /// <summary>
    /// Initialize default field values for a specific EventHandler type
    /// </summary>
    public void InitializeFieldsForType(HandlerItem item, string typeName)
    {
        var fields = ParentVm.SchemaService?.GetAllEmbeddedClassFields(typeName);
        if (fields == null) return;

        // Clear existing non-_type fields
        var keysToRemove = item.Data.Keys.Where(k => k != "_type").ToList();
        foreach (var key in keysToRemove)
            item.Data.Remove(key);

        // Add default values for all fields
        foreach (var field in fields)
        {
            if (field.Name == "_type") continue;

            item.Data[field.Name] = GetDefaultValueForField(field);
        }
    }

    /// <summary>
    /// Get default value for a field based on its type
    /// </summary>
    private object? GetDefaultValueForField(Services.SchemaService.FieldMeta field)
    {
        return field.Category switch
        {
            "primitive" => field.Type.ToLowerInvariant() switch
            {
                "int" or "int32" => 0L,
                "float" or "single" or "double" => 0.0,
                "bool" or "boolean" => false,
                "string" => "",
                _ => null
            },
            "enum" => 0L,
            "reference" => "",
            "collection" => new List<object>(),
            _ => null
        };
    }

    /// <summary>
    /// Build JSON result from current handlers
    /// </summary>
    public JsonElement BuildResult()
    {
        using var ms = new MemoryStream();
        using (var writer = new Utf8JsonWriter(ms, new JsonWriterOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }))
        {
            writer.WriteStartArray();
            foreach (var handler in Handlers)
            {
                writer.WriteStartObject();
                foreach (var kvp in handler.Data.OrderBy(k => k.Key == "_type" ? 0 : 1))
                {
                    writer.WritePropertyName(kvp.Key);
                    WriteJsonValue(writer, kvp.Value);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        using var doc = JsonDocument.Parse(ms.ToArray());
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Write a value to JSON writer
    /// </summary>
    private static void WriteJsonValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string s:
                writer.WriteStringValue(s);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case float f:
                writer.WriteNumberValue(f);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            case JsonElement je:
                je.WriteTo(writer);
                break;
            case List<object> list:
                writer.WriteStartArray();
                foreach (var item in list)
                    WriteJsonValue(writer, item);
                writer.WriteEndArray();
                break;
            case Dictionary<string, object?> dict:
                writer.WriteStartObject();
                foreach (var kvp in dict)
                {
                    writer.WritePropertyName(kvp.Key);
                    WriteJsonValue(writer, kvp.Value);
                }
                writer.WriteEndObject();
                break;
            default:
                writer.WriteStringValue(value.ToString());
                break;
        }
    }

    /// <summary>
    /// Clone a JsonElement to an object
    /// </summary>
    private static object? CloneJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? (object)l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.Clone(),
            JsonValueKind.Object => element.Clone(),
            _ => element.Clone()
        };
    }

    /// <summary>
    /// Extract string value from various object types
    /// </summary>
    private static string? ExtractStringValue(object? value)
    {
        return value switch
        {
            string s => s,
            JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString(),
            _ => value?.ToString()
        };
    }
}

/// <summary>
/// Represents a single EventHandler in the array
/// </summary>
public class HandlerItem
{
    public int Index { get; set; }
    public string? TypeName { get; set; }
    public Dictionary<string, object?> Data { get; set; } = new();
}
