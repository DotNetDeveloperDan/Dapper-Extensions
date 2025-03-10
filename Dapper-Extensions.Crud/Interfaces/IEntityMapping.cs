namespace Dapper_Extensions.Crud.Interfaces
{
    namespace DapperCrudLibrary.Mapping
    {
        public interface IEntityMapping<T>
        {
            string TableName { get; }
            IEnumerable<string> KeyProperties { get; }
        }
    }
}