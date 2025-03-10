using System.Data;
using Dapper;

namespace Dapper_Extensions.Crud.Extensions;

public class SqlServerEnumTypeHandler<T> : SqlMapper.TypeHandler<T> where T : struct, Enum
{
    public override void SetValue(IDbDataParameter parameter, T value)
    {
        // Convert enum to string representation.
        parameter.Value = value.ToString();
    }

    public override T Parse(object value)
    {
        // Parse the string back into the enum.
        return (T)Enum.Parse(typeof(T), value.ToString(), true);
    }
}