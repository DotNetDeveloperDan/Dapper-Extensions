using Dapper_Extensions.Crud.Interfaces.DapperCrudLibrary.Mapping;

namespace Dapper_Extensions.Crud;

public static class EntityMappingRegistry
{
    private static readonly Dictionary<Type, object> Mappings = new();

    public static void Register<T>(IEntityMapping<T> mapping)
    {
        Mappings[typeof(T)] = mapping;
    }

    public static IEntityMapping<T>? GetMapping<T>()
    {
        if (Mappings.TryGetValue(typeof(T), out var mapping))
        {
            return mapping as IEntityMapping<T>;
        }

        throw new Exception($"No mapping registered for {typeof(T).Name}");
    }
}