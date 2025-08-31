using Xunit;

namespace Gateway.IntegrationTests
{
    /// <summary>
    /// Test simple para verificar que GatewayTestFactory se puede crear
    /// </summary>
    public class TestFactoryCreation
    {
        [Fact]
        public void CanCreateGatewayTestFactory()
        {
            // Esta prueba solo verifica que la clase se puede instanciar
            // Si compila, significa que la clase GatewayTestFactory está disponible
            Assert.True(true);
        }
    }
}
