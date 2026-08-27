using System;
using DotNetResiliencePipeline.Exceptions;
using Xunit;

namespace DotNetResiliencePipeline.Tests
{
    /// <summary>
    /// Contains unit tests for the HttpClientException and related exception classes.
    /// </summary>
    public class HttpClientExceptionTests
    {
        /// <summary>
        /// Verifies that the HttpClientException constructor sets the Message, ClientName, and RequestUrl properties correctly when provided.
        /// </summary>
        [Fact]
        public void HttpClientException_Constructor_SetsProperties()
        {
            // Arrange
            var message = "Test message";
            var clientName = "TestClient";
            var requestUrl = "https://example.com/api";

            // Act
            var ex = new HttpClientException(message, clientName, requestUrl);

            // Assert
            Assert.Equal(message, ex.Message);
            Assert.Equal(clientName, ex.ClientName);
            Assert.Equal(requestUrl, ex.RequestUrl);
            Assert.Null(ex.InnerException);
        }

        /// <summary>
        /// Verifies that the HttpClientException constructor correctly sets the InnerException when provided.
        /// </summary>
        [Fact]
        public void HttpClientException_Constructor_WithInnerException_SetsInnerException()
        {
            // Arrange
            var message = "Test message";
            var inner = new InvalidOperationException("Inner exception");
            var clientName = "TestClient";
            var requestUrl = "https://example.com/api";

            // Act
            var ex = new HttpClientException(message, inner, clientName, requestUrl);

            // Assert
            Assert.Equal(message, ex.Message);
            Assert.Equal(inner, ex.InnerException);
            Assert.Equal(clientName, ex.ClientName);
            Assert.Equal(requestUrl, ex.RequestUrl);
        }

        /// <summary>
        /// Verifies that the InvalidHttpRequestException constructor correctly sets the HttpMethod property.
        /// </summary>
        [Fact]
        public void InvalidHttpRequestException_Constructor_SetsHttpMethod()
        {
            // Arrange
            var message = "Invalid request";
            var clientName = "ClientA";
            var requestUrl = "https://example.com";
            var httpMethod = "POST";

            // Act
            var ex = new InvalidHttpRequestException(message, clientName, requestUrl, httpMethod);

            // Assert
            Assert.Equal(message, ex.Message);
            Assert.Equal(clientName, ex.ClientName);
            Assert.Equal(requestUrl, ex.RequestUrl);
            Assert.Equal(httpMethod, ex.HttpMethod);
        }

        /// <summary>
        /// Verifies that the HttpResponseException constructor correctly sets the StatusCode property.
        /// </summary>
        [Fact]
        public void HttpResponseException_Constructor_SetsStatusCode()
        {
            // Arrange
            var message = "Bad response";
            var statusCode = 404;
            var clientName = "ClientB";
            var requestUrl = "https://example.com/resource";

            // Act
            var ex = new HttpResponseException(message, statusCode, clientName, requestUrl);

            // Assert
            Assert.Equal(message, ex.Message);
            Assert.Equal(statusCode, ex.StatusCode);
            Assert.Equal(clientName, ex.ClientName);
            Assert.Equal(requestUrl, ex.RequestUrl);
        }

        /// <summary>
        /// Verifies that the HttpTimeoutException constructor correctly sets the Timeout property.
        /// </summary>
        [Fact]
        public void HttpTimeoutException_Constructor_SetsTimeout()
        {
            // Arrange
            var message = "Timeout occurred";
            var timeout = TimeSpan.FromSeconds(30);
            var clientName = "ClientC";
            var requestUrl = "https://example.com/timeout";

            // Act
            var ex = new HttpTimeoutException(message, timeout, clientName, requestUrl);

            // Assert
            Assert.Equal(message, ex.Message);
            Assert.Equal(timeout, ex.Timeout);
            Assert.Equal(clientName, ex.ClientName);
            Assert.Equal(requestUrl, ex.RequestUrl);
        }

        /// <summary>
        /// Verifies that the ClientName and RequestUrl properties of HttpClientException are nullable and default to null.
        /// </summary>
        [Fact]
        public void HttpClientException_Properties_AreNullable()
        {
            // Arrange & Act
            var ex = new HttpClientException("msg");

            // Assert
            Assert.Null(ex.ClientName);
            Assert.Null(ex.RequestUrl);
        }

        /// <summary>
        /// Verifies that the ClientName and RequestUrl properties of HttpClientException can be set after object construction.
        /// </summary>
        [Fact]
        public void HttpClientException_Properties_CanBeSetAfterConstruction()
        {
            // Arrange
            var ex = new HttpClientException("msg");

            // Act
            ex.ClientName = "NewClient";
            ex.RequestUrl = "https://new.url";

            // Assert
            Assert.Equal("NewClient", ex.ClientName);
            Assert.Equal("https://new.url", ex.RequestUrl);
        }

        /// <summary>
        /// Verifies that derived exception classes inherit from HttpClientException and ResiliencyException.
        /// </summary>
        [Fact]
        public void DerivedException_InheritsFromHttpClientException()
        {
            // Arrange
            var ex = new InvalidHttpRequestException("msg");

            // Act & Assert
            Assert.IsAssignableFrom<HttpClientException>(ex);
            Assert.IsAssignableFrom<ResiliencyException>(ex);
        }
    }
}
