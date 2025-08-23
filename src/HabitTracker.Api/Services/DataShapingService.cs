using System.Collections.Concurrent;
using System.Dynamic;
using System.Globalization;
using System.Reflection;
using HabitTracker.Api.DTOs.Common;

namespace HabitTracker.Api.Services;

public sealed class DataShapingService
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertiesCache = new();

    public ExpandoObject ShapeData<T>(T entity, string? fields)
    {
        PropertyInfo[] propertyInfos = GetRquestedPropertyInfos(fields, typeof(T));
        
        IDictionary<string, object?> shapedObject = new ExpandoObject();

        foreach (var property in propertyInfos)
        {
            shapedObject[property.Name] = property.GetValue(entity);
        }

        return (ExpandoObject)shapedObject;
    }
    public List<ExpandoObject> ShapeDataCollection<T>(IEnumerable<T> entities, string? fields, Func<T, List<LinkDto>>? linksFactory = null)
    {
        PropertyInfo[] propertyInfos = GetRquestedPropertyInfos(fields, typeof(T));

        List<ExpandoObject> shapedObjects = [];
        foreach (var entity in entities)
        {
            IDictionary<string, object?> shapedObject = new ExpandoObject();

            shapedObjects.Add((ExpandoObject)shapedObject);

            foreach (var property in propertyInfos)
            {
                shapedObject[property.Name] = property.GetValue(entity);
            }

            if(linksFactory != null)
            {
                shapedObject["links"] = linksFactory(entity);
            }
        }

        return shapedObjects;
    }

    public bool Validate<T>(string? fields)
    {
        if (string.IsNullOrWhiteSpace(fields))
        {
            return true;
        }

        var fieldsSet = GetFieldsSet(fields);

        PropertyInfo[] propertyInfos = GeOrAddPropertyInfos(typeof(T));

        return fieldsSet.All(f => propertyInfos.Any(p => p.Name.Equals(f, StringComparison.OrdinalIgnoreCase)));
    }

    private PropertyInfo[] GetRquestedPropertyInfos(string? fields, Type currentType)
    {
        HashSet<string> fieldsSet = GetFieldsSet(fields);

        PropertyInfo[] propertyInfos = GeOrAddPropertyInfos(currentType);

        if (fieldsSet.Any())
        {
            propertyInfos = propertyInfos
                .Where(p => fieldsSet.Contains(p.Name))
                .ToArray();
        }

        return propertyInfos;
    }

    private HashSet<string> GetFieldsSet(string? fields)
    {
         return fields?
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
    }

    private PropertyInfo[] GeOrAddPropertyInfos(Type key)
    {
        return _propertiesCache.GetOrAdd(
          key,
          type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance));
    }
}
