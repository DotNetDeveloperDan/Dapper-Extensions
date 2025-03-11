using System.Collections;
using System.Reflection;
using Dapper_Extensions.Crud.Interfaces.DapperCrudLibrary.Mapping;

namespace Dapper_Extensions.Crud.Tests;

public class EntityMappingRegistryTests
{
    public EntityMappingRegistryTests()
    {
        ClearMappings();
    }

    /// <summary>
    ///     Clears the static Mappings dictionary using reflection.
    /// </summary>
    private void ClearMappings()
    {
        var field = typeof(EntityMappingRegistry).GetField("Mappings", BindingFlags.NonPublic | BindingFlags.Static);
        if (field != null)
        {
            var dict = field.GetValue(null) as IDictionary;
            dict?.Clear();
        }
    }

    [Fact]
    public void Register_ShouldStoreMapping_And_GetMapping_ShouldReturnMapping()
    {
        // Arrange
        var mapping = new DummyMapping();
        EntityMappingRegistry.Register(mapping);

        // Act
        var result = EntityMappingRegistry.GetMapping<DummyEntity>();

        // Assert
        result.Should().BeSameAs(mapping);
    }

    [Fact]
    public void GetMapping_ShouldThrowException_WhenMappingNotRegistered()
    {
        // Act
        var act = () => EntityMappingRegistry.GetMapping<DummyEntity>();

        // Assert
        act.Should().Throw<Exception>()
            .WithMessage("No mapping registered for DummyEntity");
    }

    [Fact]
    public void Register_ShouldOverwriteExistingMapping()
    {
        // Arrange
        var mapping1 = new DummyMapping();
        var mapping2 = new DummyMapping2();
        EntityMappingRegistry.Register(mapping1);
        EntityMappingRegistry.Register(mapping2);

        // Act
        var result = EntityMappingRegistry.GetMapping<DummyEntity>();

        // Assert
        result.Should().BeSameAs(mapping2);
    }

    // Define a dummy entity.
    private class DummyEntity
    {
    }

    // Define a dummy mapping that implements IEntityMapping<DummyEntity>.
    private class DummyMapping : IEntityMapping<DummyEntity>
    {
        public string TableName { get; } = string.Empty;
        public IEnumerable<string> KeyProperties { get; }
    }

    // Another dummy mapping to test overwrite behavior.
    private class DummyMapping2 : IEntityMapping<DummyEntity>
    {
        public string TableName { get; } = string.Empty;
        public IEnumerable<string> KeyProperties { get; }
    }
}