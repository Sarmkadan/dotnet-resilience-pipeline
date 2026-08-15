using DotNetResiliencePipeline.Utilities;
using Xunit;

namespace DotNetResiliencePipeline.Tests
{
    public class MetricsAggregatorJsonExtensionsTests
    {
        [Fact]
        public void ToJson_HappyPath_ReturnsJsonString()
        {
            // Arrange
            var metricsAggregator = new MetricsAggregator();

            // Act
            var json = metricsAggregator.ToJson();

            // Assert
            Assert.NotEmpty(json);
        }

        [Fact]
        public void FromJson_HappyPath_ReturnsMetricsAggregator()
        {
            // Arrange
            var json = "{\"property\":\"value\"}";

            // Act
            var metricsAggregator = MetricsAggregatorJsonExtensions.FromJson(json);

            // Assert
            Assert.NotNull(metricsAggregator);
        }

        [Fact]
        public void FromJson_NullInput_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => MetricsAggregatorJsonExtensions.FromJson(null));
        }

        [Fact]
        public void FromJson_EmptyString_ThrowsArgumentException()
        {
            // Act and Assert
            Assert.Throws<ArgumentException>(() => MetricsAggregatorJsonExtensions.FromJson(string.Empty));
        }

        [Fact]
        public void TryFromJson_HappyPath_ReturnsTrueAndMetricsAggregator()
        {
            // Arrange
            var json = "{\"property\":\"value\"}";

            // Act
            var success = MetricsAggregatorJsonExtensions.TryFromJson(json, out var metricsAggregator);

            // Assert
            Assert.True(success);
            Assert.NotNull(metricsAggregator);
        }

        [Fact]
        public void TryFromJson_NullInput_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => MetricsAggregatorJsonExtensions.TryFromJson(null, out _));
        }

        [Fact]
        public void TryFromJson_EmptyString_ThrowsArgumentException()
        {
            // Act and Assert
            Assert.Throws<ArgumentException>(() => MetricsAggregatorJsonExtensions.TryFromJson(string.Empty, out _));
        }
    }
}
