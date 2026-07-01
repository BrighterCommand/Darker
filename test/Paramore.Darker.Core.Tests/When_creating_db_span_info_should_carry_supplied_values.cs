using System.Collections.Generic;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_creating_db_span_info_should_carry_supplied_values
    {
        [Fact]
        public void Should_expose_required_positional_properties()
        {
            // Arrange
            var dbSystem = DbSystem.PostgreSql;
            const string dbName = "orders";
            const string dbOperation = "select";
            const string dbTable = "order";

            // Act
            var info = new DbSpanInfo(dbSystem, dbName, dbOperation, dbTable);

            // Assert
            info.DbSystem.ShouldBe(DbSystem.PostgreSql);
            info.DbName.ShouldBe("orders");
            info.DbOperation.ShouldBe("select");
            info.DbTable.ShouldBe("order");
        }

        [Fact]
        public void Should_default_optional_members_to_null()
        {
            // Arrange / Act
            var info = new DbSpanInfo(DbSystem.PostgreSql, "orders", "select");

            // Assert
            info.DbTable.ShouldBeNull();
            info.ServerAddress.ShouldBeNull();
            info.DbStatement.ShouldBeNull();
            info.DbUser.ShouldBeNull();
            info.DbAttributes.ShouldBeNull();
        }

        [Fact]
        public void Should_allow_optional_members_to_be_set_via_init()
        {
            // Arrange
            var attributes = new Dictionary<string, string> { ["custom.key"] = "value" };

            // Act
            var info = new DbSpanInfo(DbSystem.PostgreSql, "orders", "select")
            {
                ServerAddress = "db.example.com",
                DbStatement = "SELECT * FROM order",
                DbUser = "app_user",
                DbAttributes = attributes
            };

            // Assert
            info.ServerAddress.ShouldBe("db.example.com");
            info.DbStatement.ShouldBe("SELECT * FROM order");
            info.DbUser.ShouldBe("app_user");
            info.DbAttributes.ShouldBeSameAs(attributes);
        }
    }
}
