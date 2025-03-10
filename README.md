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

## Getting Started
- DI Registration
The library provides an extension method (AddDapperCrud) that registers:

An IDbConnection (created and opened automatically),
The generic IRepository<T>,
The IUnitOfWork.
For PostgreSQL, you can also supply a dictionary of enum mappings (key: C# enum type, value: PostgreSQL enum type name). This extension uses the new per-connection API (e.g. RegisterEnumMapping<TEnum>(string pgEnumName)) to register enum mappings.
