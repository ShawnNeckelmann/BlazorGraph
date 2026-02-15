using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Remote;
using Gremlin.Net.Process.Traversal;
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
        await _g.V(lp).AddE("purchased").To(p1).Promise(t => t.Iterate());
        await _g.V(bp).AddE("purchased").To(p1).Promise(t => t.Iterate());
        await _g.V(bp).AddE("purchased").To(p2).Promise(t => t.Iterate());

        // 4. Verify by counting vertices
        var count = await _g.V().HasLabel("person").Count().Promise(t => t.Next());
        if (count != 2) throw new Exception($"Expected 2 person vertices, but found {count}");
    }
}