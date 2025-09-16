using Xunit;

namespace Gateway.UnitTests
{
    public class SimpleTest
    {
        [Fact]
        public void Simple_Test_Should_Pass()
        {
            // Arrange
            var result = true;

            // Assert
            Assert.True(result);
        }
    }
}
