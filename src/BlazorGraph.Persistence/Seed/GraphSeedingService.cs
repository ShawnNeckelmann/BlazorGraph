using Bogus;
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Remote;
using Gremlin.Net.Process.Traversal;
using Gremlin.Net.Structure;
using static Gremlin.Net.Process.Traversal.AnonymousTraversalSource;

namespace BlazorGraph.Persistence.Seed;

public class GraphSeedingService(GremlinClient client)
{
    private readonly GraphTraversalSource _g = Traversal().With(new DriverRemoteConnection(client));

    public async Task SeedAsync()
    {
        // 1. Wipe existing data
        await _g.V().Drop().Promise(t => t.Iterate());

        // 2. Add Vertices using the Fluent API
        var lp = await _g.AddV("person").Property("name", "Lilou").Promise(t => t.Next());
        var bp = await _g.AddV("person").Property("name", "Bijou").Promise(t => t.Next());

        var p1 = await _g.AddV("product").Property("name", "Bluey Book").Promise(t => t.Next());
        var p2 = await _g.AddV("product").Property("name", "Sticker Set").Promise(t => t.Next());

        // 3. Add Edges
        await _g
            .V(lp).AddE("purchased").To(p1)
            .V(bp).AddE("purchased").To(p1)
            .V(bp).AddE("purchased").To(p2)
            .Promise(t => t.Iterate());

        // 4. Verify by counting vertices
        var count = await _g.V().HasLabel("person").Count().Promise(t => t.Next());
        if (count != 2) throw new Exception($"Expected 2 person vertices, but found {count}");
    }

    public async Task SeedAsync(int userCount, int productCount)
    {
        var faker = new Faker();

        // 1. Generate Products first so users have something to buy
        var productIds = new List<Vertex>();
        for (var i = 0; i < productCount; i++)
        {
            var p = await _g.AddV("product")
                .Property("name", faker.Commerce.ProductName())
                .Promise(traversal => traversal.Next());

            if (p == null) throw new Exception("Failed to create product vertex");
            productIds.Add(p);
        }

        // 2. Generate Users and link them to random products
        for (var i = 0; i < userCount; i++)
        {
            var user = _g.AddV("person")
                .Property("name", faker.Name.FullName())
                .Next();

            // Pick 5-10 random products for this user
            var randomProducts = faker.PickRandom(productIds, faker.Random.Number(5, 10));

            foreach (var prod in randomProducts)
                await _g
                    .V(user)
                    .AddE("purchased")
                    .To(prod)
                    .Promise(traversal => traversal.Iterate());
        }
    }

    /// <summary>
    ///     Completely wipes the database and seeds it with products, users, and purchase history.
    /// </summary>
    public async Task SeedAsync(int userCount)
    {
        // 1. DROP THE ENTIRE DATABASE 💣
        await _g.V().Drop().Promise(t => t.Iterate());

        var faker = new Faker();
        var productMap = new Dictionary<string, List<ProductInfo>>();
        var categories = new[] { "Development", "Cloud", "DevOps", "Data Science" };

        // 2. CREATE PRODUCTS FIRST 🏛️
        // We create a baseline of products so we have something for users to buy.
        foreach (var category in categories)
        {
            productMap[category] = [];

            for (var i = 0; i < 10; i++) // 10 products per category
            {
                var productName = $"{category} {faker.Commerce.ProductName()}";

                // Create product vertex
                var productVertex = await _g.AddV("product")
                    .Property("name", productName)
                    .Property("category", category)
                    .Property("price", decimal.Parse(faker.Commerce.Price()))
                    .Promise(t => t.Next());

                if (productVertex is null) continue;

                productMap[category].Add(new ProductInfo
                {
                    Id = productVertex.Id,
                    Category = category
                });
            }
        }

        // 3. CREATE USERS AND EDGES 👤⛓️
        for (var i = 0; i < userCount; i++)
        {
            var persona = faker.PickRandom(categories);
            var userName = faker.Name.FullName();

            // Create User Vertex
            var userVertex = await _g.AddV("person")
                .Property("name", userName)
                .Property("persona", persona)
                .Promise(t => t.Next());

            // Assign 5 products per user using the 80/20 Weighted Logic
            for (var j = 0; j < 5; j++)
            {
                var preferredCategory = persona;

                // 20% chance to deviate from their persona (creates the "messy" data needed for discovery)
                if (faker.Random.Bool(0.2f)) preferredCategory = faker.PickRandom(categories);

                if (!productMap.TryGetValue(preferredCategory, out var availableProducts)) continue;

                var chosenProduct = faker.PickRandom(availableProducts);

                // Create the "purchased" edge
                await _g.V(userVertex.Id)
                    .AddE("purchased")
                    .To(__.V(chosenProduct.Id))
                    .Promise(t => t.Iterate());
            }
        }
    }


    private class ProductInfo
    {
        public object Id { get; set; } = default!;
        public string Category { get; set; } = string.Empty;
    }
}