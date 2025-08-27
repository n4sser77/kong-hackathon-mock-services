using Bogus;

namespace ProductCatalog
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add Swagger services
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();

            var productFaker = new Faker<Product>()
                .CustomInstantiator(f => new Product(0, "", "", 0m, false)) // seed with defaults
                .RuleFor(p => p.Id, f => f.IndexFaker + 1)
                .RuleFor(p => p.Name, f => f.Commerce.ProductName())
                .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
                .RuleFor(p => p.Price, f => decimal.Parse(f.Commerce.Price(10, 200)))
                .RuleFor(p => p.Available, f => f.Random.Bool(0.8f));

            var products = productFaker.Generate(20);

            app.MapGet("/products", () => Results.Ok(products));

            app.MapGet("/products/{productId:int}", (int productId) =>
            {
                var product = products.FirstOrDefault(p => p.Id == productId);
                return product is not null ? Results.Ok(product) : Results.NotFound();
            });

            app.Run();
        }
    }

    public record Product(int Id, string Name, string Description, decimal Price, bool Available);
}