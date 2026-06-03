namespace NewGest.IntegrationTests;

/// <summary>
/// Define la colección de tests de integración.
/// Todos los tests de la colección comparten la misma instancia de
/// NewgestWebApplicationFactory y la BD se inicializa una sola vez.
/// </summary>
[CollectionDefinition("IntegrationTests")]
public class IntegrationTestsCollection : ICollectionFixture<NewgestWebApplicationFactory>
{
    // Esta clase actúa solo como marcador para xUnit.
    // La inicialización real de la BD ocurre en el fixture.
}
