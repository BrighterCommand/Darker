using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_mapping_db_system_should_return_otel_string
    {
        [Fact]
        public void Should_map_postgresql_to_otel_string()
        {
            // Arrange
            var system = DbSystem.PostgreSql;

            // Act
            var result = system.ToDbSystemString();

            // Assert
            result.ShouldBe("postgresql");
        }

        [Fact]
        public void Should_map_mssql_to_otel_string()
        {
            // Arrange
            var system = DbSystem.MsSql;

            // Act
            var result = system.ToDbSystemString();

            // Assert
            result.ShouldBe("mssql");
        }

        [Fact]
        public void Should_map_mysql_to_otel_string()
        {
            // Arrange
            var system = DbSystem.MySql;

            // Act
            var result = system.ToDbSystemString();

            // Assert
            result.ShouldBe("mysql");
        }

        [Fact]
        public void Should_map_sqlite_to_otel_string()
        {
            // Arrange
            var system = DbSystem.Sqlite;

            // Act
            var result = system.ToDbSystemString();

            // Assert
            result.ShouldBe("sqlite");
        }

        [Fact]
        public void Should_return_non_null_fallback_for_other()
        {
            // Arrange
            var system = DbSystem.Other;

            // Act
            var result = system.ToDbSystemString();

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBe("other_sql");
        }
    }
}
