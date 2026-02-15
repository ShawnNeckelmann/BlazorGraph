using Gremlin.Net.Driver;

namespace BlazorGraph.Persistence.Seed;

public class GraphSeedingService(GremlinClient client)
{
    public async Task SeedAsync()
    {
        // 1. Wipe existing data to ensure a clean state
        await client.SubmitAsync<dynamic>("g.V().drop().iterate()");

        // 2. Add our family and products
        // We use a single string to reduce round-trips to the server
        const string query = """

                                         g.addV('person').property('name', 'Lilou').as('lp')
                                          .addV('person').property('name', 'Bijou').as('bp')
                                          .addV('product').property('name', 'Bluey Book').as('p1')
                                          .addV('product').property('name', 'Sticker Set').as('p2')
                                          .addE('purchased').from('lp').to('p1')
                                          .addE('purchased').from('bp').to('p1')
                                          .addE('purchased').from('bp').to('p2')
                                          .iterate()
                             """;

        await client.SubmitAsync<dynamic>(query);
    }
}