# Dapper-Extensions.Crud

Dapper-Extensions.Crud is a lightweight data access library implementing a generic repository and unit-of-work pattern using Dapper and Dapper.Contrib. It offers both synchronous and asynchronous CRUD operations, upsert (including batched upsert), and seamless integration with dependency injection (DI). The library supports SQL Server and PostgreSQL—including native PostgreSQL enum mapping using the per‑connection API in Npgsql 7.

## Features

- **Generic Repository Pattern:** Simplify CRUD operations on your entities.
- **Unit-of-Work Pattern:** Coordinate multiple repository operations within a single transaction.
- **Asynchronous Support:** Async versions of CRUD and upsert methods.
- **Batched Upsert:** Efficient bulk upsert operations.
- **Multi-Database Support:** Works with SQL Server and PostgreSQL.
- **PostgreSQL Enum Mapping:** Supports native PostgreSQL enum types using per‑connection mapping.
- **DI Integration:** Provides extension methods to register all services with one call.

## Installation

Install via NuGet:

```bash
dotnet add package Dapper-Extensions.Crud
```
## Getting Started
- **DI Registration
The library provides an extension method (AddDapperCrud) that registers:

- An IDbConnection (created and opened automatically)
- The generic IRepository<T>
- The IUnitOfWork
- For PostgreSQL, you can also supply a dictionary of enum mappings (key: C# enum type, value: PostgreSQL enum type name). The DI extension uses the new per‑connection API (e.g. RegisterEnumMapping<TEnum>(string pgEnumName)) to register each mapping before opening the connection.

```bash
using Dapper_Extensions.Crud;
using Dapper_Extensions.Crud.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

// Example enum.
public enum OrderStatus
{
    Pending,
    Completed,
    Cancelled
}

var builder = WebApplication.CreateBuilder(args);

// Retrieve connection string from configuration.
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Prepare enum mappings for PostgreSQL.
var enumMappings = new Dictionary<Type, string>
{
    { typeof(OrderStatus), "order_status" }
};

// Register Dapper CRUD services.
// Use DatabaseProvider.PostgreSQL for PostgreSQL or DatabaseProvider.SqlServer for SQL Server.
builder.Services.AddDapperCrud(connectionString, DatabaseProvider.PostgreSQL, enumMappings);

var app = builder.Build();
app.MapControllers();
app.Run();
```

- **Using the Repository and UnitOfWork
After DI registration, you can inject IUnitOfWork or IRepository<T> into your controllers or services.

Sample Controller

```bash
using Dapper_Extensions.Crud.Interfaces;
using Microsoft.AspNetCore.Mvc;
using YourApp.Models; // Your domain models

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<Order> _orderRepository;

    public OrdersController(IUnitOfWork unitOfWork, IRepository<Order> orderRepository)
    {
        _unitOfWork = unitOfWork;
        _orderRepository = orderRepository;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderAsync(int id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
        {
            return NotFound();
        }
        return Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrderAsync(Order newOrder)
    {
        _unitOfWork.BeginTransaction();
        try
        {
            var orderId = await _orderRepository.AddAsync(newOrder);
            _unitOfWork.Commit();
            return CreatedAtAction(nameof(GetOrderAsync), new { id = orderId }, newOrder);
        }
        catch (Exception)
        {
            _unitOfWork.Rollback();
            return StatusCode(500, "An error occurred while creating the order.");
        }
    }
}
```
- **Upsert and Batch Upsert
The library provides asynchronous upsert methods that support both single and composite key entities, as well as batched upsert for bulk operations.

Upsert a Single Entity

```bash
// Upsert a single entity.
var upsertResult = await _orderRepository.UpsertAsync(order);
```
- **Batch Upsert a List of Entities
```bash
// Upsert a list of entities in batches (e.g., 500 at a time).
var (inserted, updated) = await _orderRepository.UpsertListBatchAsync(orderList, batchSize: 500);
```

- **Database-Specific Details
- SQL Server
Default Behavior:
By default, .NET enums are stored as their underlying integer values. Dapper automatically handles this conversion.

Custom Enum Mapping (Optional):
If you prefer to store SQL Server enums as strings, you can create and register a custom Dapper type handler. For example:
```bash
using System;
using System.Data;
using Dapper;

public class SqlServerEnumTypeHandler<T> : SqlMapper.TypeHandler<T> where T : struct, Enum
{
    public override void SetValue(IDbDataParameter parameter, T value)
    {
        parameter.Value = value.ToString();
    }

    public override T Parse(object value)
    {
        return (T)Enum.Parse(typeof(T), value.ToString(), ignoreCase: true);
    }
}
```
- PostgreSQL
Native Enum Types:
PostgreSQL supports native enum types. To leverage this, register your enum mappings on each connection using the new per‑connection API in Npgsql 7.

Generic Enum Mapping:
The DI extension accepts a dictionary of enum mappings. For each mapping, the connection calls RegisterEnumMapping<TEnum>(string pgEnumName) before opening, ensuring that PostgreSQL stores and retrieves the enum as its native type.

Custom PostgreSQL Enum Type Handler (Optional)
If you need additional control over enum conversion, you can create a custom Dapper type handler:

```bash
using System;
using System.Data;
using Dapper;

public class PostgreSQLEnumTypeHandler<T> : SqlMapper.TypeHandler<T> where T : struct, Enum
{
    public override void SetValue(IDbDataParameter parameter, T value)
    {
        parameter.Value = value.ToString();
    }

    public override T Parse(object value)
    {
        return (T)Enum.Parse(typeof(T), value.ToString(), ignoreCase: true);
    }
}
```

Register this type handler as part of your enum mappings if desired.

Contributing
Contributions are welcome! Fork the repository, make your changes, and submit a pull request. For major changes, please open an issue first to discuss your ideas.

