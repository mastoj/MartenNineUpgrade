using Marten;

var connectionString = Environment.GetEnvironmentVariable("MARTEN_CONNECTION_STRING")
    ?? "Host=localhost;Port=5432;Database=marten_nine;Username=postgres;Password=postgres";

await using var store = DocumentStore.For(options =>
{
    options.Connection(connectionString);
    options.AutoCreateSchemaObjects = JasperFx.AutoCreate.All;
    options.Projections.Snapshot<MyType>(JasperFx.Events.Projections.SnapshotLifecycle.Inline);
});

await store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();

var streamId = Guid.NewGuid();
var command = new CreateMyType(streamId, "test");
var events = Decide(command, null).ToArray();

await using (var session = store.LightweightSession())
{
    session.Events.StartStream<MyType>(streamId, events);
    await session.SaveChangesAsync();
}

await using (var querySession = store.QuerySession())
{
    var snapshot = await querySession.LoadAsync<MyType>(streamId);

    Console.WriteLine($"Snapshot found: {snapshot is not null}");
    Console.WriteLine($"Name: {snapshot?.Name}");
    Console.WriteLine($"Count: {snapshot?.Count}");
    Console.WriteLine($"LastEvent: {snapshot?.LastEvent}");
}

static IReadOnlyList<object> Decide(CreateMyType command, MyType? current)
{
    if (current is not null)
    {
        throw new InvalidOperationException("MyType already exists.");
    }

    return
    [
        new MyTypeCreated(command.Id, command.Name),
        new MyTypeIncremented(1)
    ];
}

public record CreateMyType(Guid Id, string Name);

public record MyTypeCreated(Guid Id, string Name);

public record MyTypeIncremented(int Amount);

public record MyType(Guid Id, string Name, int Count, string LastEvent)
{
    public static MyType Create(MyTypeCreated @event)
    {
        return new MyType(@event.Id, @event.Name, 0, nameof(MyTypeCreated));
    }

    public static MyType Apply(MyTypeIncremented @event, MyType current)
    {
        return current with
        {
            Count = current.Count + @event.Amount,
            LastEvent = nameof(MyTypeIncremented)
        };
    }
}
