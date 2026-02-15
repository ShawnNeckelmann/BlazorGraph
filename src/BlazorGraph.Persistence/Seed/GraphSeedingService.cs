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
        var productVertices = new List<Vertex>(productCount);
        var productTasks = new List<Task<Vertex?>>(productCount);
        for (var i = 0; i < productCount; i++)
        {
            var task = _g.AddV("product")
                .Property("name", faker.Commerce.ProductName())
                .Promise(t => t.Next());

            productTasks.Add(task);
        }

        var products = await Task.WhenAll(productTasks);
        productVertices.AddRange(products.Where(p => p != null)!);

        // 2. Generate Users and link them to random products
        var userTasks = new List<Task<Vertex?>>(userCount);
        for (var i = 0; i < userCount; i++)
        {
            var task = _g.AddV("person")
                .Property("name", faker.Name.FullName())
                .Promise(t => t.Next());
            userTasks.Add(task);
        }

        var users = await Task.WhenAll(userTasks);

        // 3. Link users to random products
        var edgeTasks = new List<Task>(userCount * 10); // estimate
        foreach (var user in users)
        {
            var randomProducts = faker.PickRandom(productVertices, faker.Random.Number(5, 10));
            foreach (var prod in randomProducts)
            {
                var edgeTask = _g.V(user).AddE("purchased").To(prod).Promise(t => t.Iterate());
                edgeTasks.Add(edgeTask);
            }
        }

        await Task.WhenAll(edgeTasks);
    }
}