using System;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.AspNetCore.Tests
{
    [Collection("DarkerHostBootstrap")]
    public class AddDarkerRequestScopeGuardTests
    {
        [Fact]
        public void When_the_service_collection_is_null_should_throw_naming_the_services_parameter()
        {
            // Arrange
            IServiceCollection services = null;

            // Act
            var exception = Should.Throw<ArgumentNullException>(() => services.AddDarkerRequestScope());

            // Assert
            exception.ParamName.ShouldBe("services");
        }
    }
}
