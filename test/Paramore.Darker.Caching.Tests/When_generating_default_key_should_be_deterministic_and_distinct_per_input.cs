using Shouldly;
using Xunit;

namespace Paramore.Darker.Caching.Tests;

// Test doubles: query with UserId property for the body assertions
file record GetUser(int UserId);

// Test doubles: two types with identical property shape (int Id) for the collision assertion
file record CollisionQueryA(int Id);
file record CollisionQueryB(int Id);

public class DefaultCacheKeyGeneratorTests
{
    private readonly DefaultCacheKeyGenerator defaultCacheKeyGenerator = new();

    [Fact]
    public void When_generating_default_key_should_be_deterministic_and_distinct_per_input()
    {
        //Arrange
        var queryWith42 = new GetUser(42);
        var queryWith43 = new GetUser(43);
        var collisionA = new CollisionQueryA(42);
        var collisionB = new CollisionQueryB(42);

        //Act
        var key42 = defaultCacheKeyGenerator.GenerateKey(queryWith42);
        var key43 = defaultCacheKeyGenerator.GenerateKey(queryWith43);
        var keyA = defaultCacheKeyGenerator.GenerateKey(collisionA);
        var keyB = defaultCacheKeyGenerator.GenerateKey(collisionB);
        var key42Again = defaultCacheKeyGenerator.GenerateKey(queryWith42);

        //Assert

        // Key starts with the query type's FullName
        key42.ShouldStartWith(typeof(GetUser).FullName!);

        // Key body is exactly |{"UserId":42}
        key42.ShouldEndWith("|{\"UserId\":42}");

        // Different input value produces a different key body
        key43.ShouldEndWith("|{\"UserId\":43}");
        key43.ShouldNotBe(key42);

        // Two different query types with identical property shape and same value produce different keys
        // (proven by the Type.FullName prefix — not merely implied)
        keyA.ShouldNotBe(keyB);

        // Determinism: calling GenerateKey twice on equal inputs yields the identical string
        key42Again.ShouldBe(key42);
    }
}
