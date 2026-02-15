using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Remote;
using Gremlin.Net.Process.Traversal;
using static Gremlin.Net.Process.Traversal.AnonymousTraversalSource;

namespace BlazorGraph.Persistence.Repositories;

public class UserRepository(GremlinClient client)
{
    private readonly GraphTraversalSource _g = Traversal().With(new DriverRemoteConnection(client));

    public async Task<IList<string?>> GetRecommendationsAsync(string userName)
    {
        return await _g.V().Has("person", "name", userName)
            .As("self") // Label the starting user
            .Out("purchased").Aggregate("bought") // Collect what they already own
            .In("purchased") // Find peers who bought the same things
            .Where(P.Not(P.Eq("self"))) // Don't look at Lilou herself as a peer
            .Out("purchased") // Find what those peers bought
            .Where(P.Without("bought")) // Filter out what Lilou already owns
            .Dedup() // Remove duplicates
            .Values<string>("name") // Get the product names
            .Promise(t => t.ToList());
    }
}