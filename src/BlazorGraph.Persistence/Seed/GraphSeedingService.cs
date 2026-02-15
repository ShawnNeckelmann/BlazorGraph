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
}