---
name: csharp-expert
description: |
  Modern C# language patterns and idioms (C# 12+). Use when:
  (1) Writing or refactoring C# code with modern syntax
  (2) Questions about pattern matching, collection expressions, primary constructors
  (3) Async/await patterns and best practices
  (4) Code style decisions (braces, expression bodies, null handling)
---

# C# Modern Patterns

## Primary Constructors

Use for dependency injection. Never use underscore-prefixed fields.

```csharp
// Correct
public class OrderService(
    IOrderRepository repository,
    ILogger<OrderService> logger) : IOrderService
{
    public async Task<Order> GetAsync(int id, CancellationToken ct) =>
        await repository.GetByIdAsync(id, ct);
}

// Wrong
public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;
    public OrderService(IOrderRepository repository) => _repository = repository;
}
```

## Pattern Matching

```csharp
// Null checks
if (user is null)
if (user is not null)

// Property patterns
if (order is { Status: OrderStatus.Complete, Total: > 100 })

// List patterns
if (items is [])                          // empty
if (items is [var first, ..])             // at least one
if (items is [var first, .., var last])   // first and last

// Or patterns
if (status is OrderStatus.Pending or OrderStatus.Processing)

// Switch expressions
return items switch
{
    [] => "Empty",
    [var single] => $"One: {single}",
    [var first, .., var last] => $"Range: {first}-{last}",
    _ => "Multiple"
};
```

## Collection Expressions

```csharp
List<string> names = [];
List<string> phones = model.Phone is not null ? [model.Phone] : [];
string[] roles = ["admin", "user"];
var combined = [..existing, ..newItems];

// Wrong
List<string> names = new List<string>();
List<string> names = new();
```

## Expression Bodies & Braces

Omit braces for single statements. Use expression bodies for simple methods.

```csharp
// Correct
if (user is null)
    return Result.Fail("Not found");

foreach (var item in items)
    total += item.Price;

public async Task<Order> GetAsync(int id, CancellationToken ct) =>
    await repository.GetByIdAsync(id, ct);

public int Count => items.Count;

// Wrong
if (user is null)
{
    return Result.Fail("Not found");
}
```

## Async/Await

```csharp
// Expression body for simple methods
public async Task<Product> GetAsync(string id, CancellationToken ct = default) =>
    await repository.GetByIdAsync(id, ct);

// Parallel execution
var stockTask = stockService.GetAsync(productId, ct);
var priceTask = priceService.GetAsync(productId, ct);
await Task.WhenAll(stockTask, priceTask);
return (await stockTask, await priceTask);

// ValueTask for hot paths with caching
public ValueTask<Product?> GetCachedAsync(string id) =>
    cache.TryGetValue(id, out Product? product)
        ? ValueTask.FromResult(product)
        : new ValueTask<Product?>(GetFromDatabaseAsync(id));
```

**Avoid:**
- `task.Result` or `.GetAwaiter().GetResult()` (deadlock risk)
- `async void` except for event handlers
- `Task.Run` for already async code

## Comments

Never generate XML summary comments. Only inline comments when logic is non-obvious.

```csharp
// Correct
public class OrderService(IOrderRepository repository) : IOrderService
{
    public async Task<Order> ProcessAsync(Order order, CancellationToken ct)
    {
        // Retry up to 3 times due to external payment gateway instability
        for (var attempt = 0; attempt < 3; attempt++)
            if ((await TryProcessPayment(order, ct)).IsSuccess)
                return order;

        return Result.Fail("Payment failed after retries");
    }
}

// Wrong - no summary comments
/// <summary>Processes an order</summary>
```

## Quick Reference

| Pattern | Use | Avoid |
|---------|-----|-------|
| DI | `class Svc(IDep dep)` | `private readonly IDep _dep;` |
| Null | `is null`, `is not null` | `== null`, `!= null` |
| Empty | `[]` | `new List<T>()`, `new()` |
| Single item | `[item]` | `new List<T> { item }` |
| Spread | `[..a, ..b]` | `a.Concat(b).ToList()` |
| Single stmt | No braces | `{ return x; }` |
| Simple method | `=>` body | Block with single return |
| Or pattern | `is A or B` | `== A \|\| == B` |
| Comments | Inline only | XML summary docs |
