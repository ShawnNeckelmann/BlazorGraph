using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Remote;
using Gremlin.Net.Process.Traversal;
using static Gremlin.Net.Process.Traversal.AnonymousTraversalSource;

namespace BlazorGraph.Persistence.Repositories;

public class UserRepository(GremlinClient client)
{
    private readonly GraphTraversalSource _g = Traversal().With(new DriverRemoteConnection(client));

    public async Task<IDictionary<string, long>> GetRecommendationsAsync(string userName)
    {
        var purchases = await _g.V().Has("person", "name", userName)
                            .As("self") // Label the starting user
                            .Out("purchased").Aggregate("bought") // Collect what they already own
                            .In("purchased") // Find peers who bought the same things
                            .Where(P.Not(P.Eq("self"))) // Don't look at Lilou herself as a peer
                            .Out("purchased") // Find what those peers bought
                            .Where(P.Without("bought")) // Filter out what Lilou already owns
                            // 2. Count occurrences and return as a Map
                            .GroupCount<string>().By("name")
                            .Promise(t => t.Next()) ??
                        new Dictionary<string, long>();

        return purchases;
    }

    public async Task<IList<string>> GetExistingUserNamesAsync()
    {
        // We start at all vertices, filter for 'person', 
        // and grab just the 'name' property values.
        var names = await _g.V().HasLabel("person")
            .Values<string>("name")
            .Order() // Let's alphabetize them for a better UI experience!
            .Promise(t => t.ToList());

        var retval = names
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => s!)
            .ToList();

        return retval;
    }
}