using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddValidation();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

// In-memory data store
var products = new List<Product>
{
    new(1, "Mechanical Keyboard", "Electronics", 149.99m, 50),
    new(2, "Wireless Mouse", "Electronics", 49.99m, 100),
    new(3, "USB-C Hub", "Accessories", 79.99m, 75)
};
var nextId = 4;

// Route group for products
var productsGroup = app.MapGroup("/api/products")
    .WithTags("Products");

// GET all products
productsGroup.MapGet("/", Results<Ok<List<Product>>, NoContent> (
    string? category,
    decimal? minPrice,
    int page = 1,
    int pageSize = 10) =>
{
    var query = products.AsEnumerable();

    if (!string.IsNullOrEmpty(category))
        query = query.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

    if (minPrice.HasValue)
        query = query.Where(p => p.Price >= minPrice);

    var result = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

    return result.Count > 0 ? TypedResults.Ok(result) : TypedResults.NoContent();
})
.WithName("GetProducts")
.WithSummary("Gets all products with optional filtering");

// GET single product
productsGroup.MapGet("/{id:int}", Results<Ok<Product>, NotFound> (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);
    return product is not null ? TypedResults.Ok(product) : TypedResults.NotFound();
})
.WithName("GetProductById")
.WithSummary("Gets a product by ID");

// POST new product
productsGroup.MapPost("/", Results<Created<Product>, BadRequest<string>> (CreateProductRequest request) =>
{
    var product = new Product(nextId++, request.Name, request.Category, request.Price, request.Stock);
    products.Add(product);
    return TypedResults.Created($"/api/products/{product.Id}", product);
})
.WithName("CreateProduct")
.WithSummary("Creates a new product");

// PUT update product
productsGroup.MapPut("/{id:int}", Results<NoContent, NotFound> (int id, UpdateProductRequest request) =>
{
    var index = products.FindIndex(p => p.Id == id);
    if (index == -1) return TypedResults.NotFound();

    products[index] = new Product(id, request.Name, request.Category, request.Price, request.Stock);
    return TypedResults.NoContent();
})
.WithName("UpdateProduct")
.WithSummary("Updates an existing product");

// DELETE product
productsGroup.MapDelete("/{id:int}", Results<NoContent, NotFound> (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);
    if (product is null) return TypedResults.NotFound();

    products.Remove(product);
    return TypedResults.NoContent();
})
.WithName("DeleteProduct")
.WithSummary("Deletes a product");

app.Run();

// Models
public record Product(int Id, string Name, string Category, decimal Price, int Stock);

public record CreateProductRequest(
    [Required, StringLength(100)] string Name,
    [Required, StringLength(50)] string Category,
    [Range(0.01, 999999.99)] decimal Price,
    [Range(0, int.MaxValue)] int Stock
);

public record UpdateProductRequest(
    [Required, StringLength(100)] string Name,
    [Required, StringLength(50)] string Category,
    [Range(0.01, 999999.99)] decimal Price,
    [Range(0, int.MaxValue)] int Stock
);