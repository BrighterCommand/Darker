using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Testing;
using Paramore.Darker.Testing.Ports;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests
{
    public class FakeQueryProcessorTests
    {
        [Fact]
        public void ReturnsExecutedQueries()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var queryProcessor = new FakeQueryProcessor();

            // Act
            queryProcessor.Execute(new TestQueryA(guid));
            queryProcessor.Execute(new TestQueryB(100));
            queryProcessor.Execute(new TestQueryB(200));
            queryProcessor.Execute(new TestQueryB(300));

            // Assert
            queryProcessor.GetExecutedQueries().Count().ShouldBe(4);

            queryProcessor.GetExecutedQueries<TestQueryA>().Count().ShouldBe(1);
            queryProcessor.GetExecutedQueries<TestQueryB>().Count().ShouldBe(3);
            queryProcessor.GetExecutedQueries<TestQueryC>().Count().ShouldBe(0);

            queryProcessor.GetExecutedQueries<TestQueryA>().Single().Id.ShouldBe(guid);
            queryProcessor.GetExecutedQueries<TestQueryB>().ElementAt(0).Number.ShouldBe(100);
            queryProcessor.GetExecutedQueries<TestQueryB>().ElementAt(1).Number.ShouldBe(200);
            queryProcessor.GetExecutedQueries<TestQueryB>().ElementAt(2).Number.ShouldBe(300);
        }

        [Fact]
        public async Task ReturnsExecutedQueriesAsync()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var queryProcessor = new FakeQueryProcessor();

            // Act
            await queryProcessor.ExecuteAsync(new TestQueryA(guid));
            await queryProcessor.ExecuteAsync(new TestQueryB(100));
            await queryProcessor.ExecuteAsync(new TestQueryB(200));
            await queryProcessor.ExecuteAsync(new TestQueryB(300));

            // Assert
            queryProcessor.GetExecutedQueries().Count().ShouldBe(4);

            queryProcessor.GetExecutedQueries<TestQueryA>().Count().ShouldBe(1);
            queryProcessor.GetExecutedQueries<TestQueryB>().Count().ShouldBe(3);
            queryProcessor.GetExecutedQueries<TestQueryC>().Count().ShouldBe(0);

            queryProcessor.GetExecutedQueries<TestQueryA>().Single().Id.ShouldBe(guid);
            queryProcessor.GetExecutedQueries<TestQueryB>().ElementAt(0).Number.ShouldBe(100);
            queryProcessor.GetExecutedQueries<TestQueryB>().ElementAt(1).Number.ShouldBe(200);
            queryProcessor.GetExecutedQueries<TestQueryB>().ElementAt(2).Number.ShouldBe(300);
        }

        [Fact]
        public void ReturnsDefaultValueOfQueriesWithoutSetup()
        {
            // Arrange
            var queryProcessor = new FakeQueryProcessor();
            queryProcessor.SetupResultFor<TestQueryB>(1234);

            // Act
            var result = queryProcessor.Execute(new TestQueryA(Guid.NewGuid()));

            // Assert
            result.ShouldBe(default(Guid));
        }

        [Fact]
        public void ReturnsValueThatWasSetUpForQuery()
        {
            // Arrange
            var queryProcessor = new FakeQueryProcessor();
            queryProcessor.SetupResultFor<TestQueryB>(1234);

            // Act
            var result = queryProcessor.Execute(new TestQueryB(1337));

            // Assert
            result.ShouldBe(1234);
        }

        [Fact]
        public void ReturnsValueThatWasSetUpForQueryWithPredicate()
        {
            // Arrange
            var queryProcessor = new FakeQueryProcessor();
            queryProcessor.SetupResultFor<TestQueryB>(q => q.Number == 1337, 1234);

            // Act
            var result = queryProcessor.Execute(new TestQueryB(1337));

            // Assert
            result.ShouldBe(1234);
        }

        [Fact]
        public void ReturnsComputedValueThatWasSetUpForQueryWithPredicate()
        {
            // Arrange
            var queryProcessor = new FakeQueryProcessor();
            queryProcessor.SetupResultFor<TestQueryB>(q => q.Number == 1337, q => (int)q.Number);

            // Act
            var result = queryProcessor.Execute(new TestQueryB(1337));

            // Assert
            result.ShouldBe(1337);
        }

        [Fact]
        public void ReturnsDefaultValueIfQueryDoesntMatchPredicate()
        {
            // Arrange
            var queryProcessor = new FakeQueryProcessor();
            queryProcessor.SetupResultFor<TestQueryB>(q => q.Number == 1337, 1234);

            // Act
            var result = queryProcessor.Execute(new TestQueryB(9999));

            // Assert
            result.ShouldBe(default(int));
        }

        [Fact]
        public void ThrowsForQueryWithThrowSetup()
        {
            // Arrange
            var queryProcessor = new FakeQueryProcessor();
            queryProcessor.SetupExceptionFor<TestQueryB>(new AbandonedMutexException());

            // Act + Assert
            Assert.Throws<AbandonedMutexException>(() => queryProcessor.Execute(new TestQueryB(1337)));
        }

        [Fact]
        public void ThrowsForQueryWithThrowSetupMatchingPredicate()
        {
            // Arrange
            var queryProcessor = new FakeQueryProcessor();
            queryProcessor.SetupExceptionFor<TestQueryB>(q => q.Number == 1337, new AbandonedMutexException());

            // Act + Assert
            Assert.Throws<AbandonedMutexException>(() => queryProcessor.Execute(new TestQueryB(1337)));
        }

        [Fact]
        public void DoesNotThrowIfQueryDoesntMatchPredicate()
        {
            // Arrange
            var queryProcessor = new FakeQueryProcessor();
            queryProcessor.SetupExceptionFor<TestQueryB>(q => q.Number == 1337, new AbandonedMutexException());

            // Act
            var result = queryProcessor.Execute(new TestQueryB(9999));

            // Assert
            result.ShouldBe(default(int));
        }

        [Fact]
        public async Task ReturnsDefaultValueOfQueriesWithoutSetupAsync()
        {
            // Arrange
            var queryProcessor = new FakeQueryProcessor();
            queryProcessor.SetupResultFor<TestQueryB>(1234);

            // Act
            var result = await queryProcessor.ExecuteAsync(new TestQueryA(Guid.NewGuid()));

            // Assert
            result.ShouldBe(default(Guid));
        }

        [Fact]
        public async Task ReturnsValueThatWasSetUpForQueryAsync()
        {
            // Arrange
            var queryProcessor = new FakeQueryProcessor();
            queryProcessor.SetupResultFor<TestQueryB>(1234);

            // Act
            var result = await queryProcessor.ExecuteAsync(new TestQueryB(1337));

            // Assert
            result.ShouldBe(1234);
        }

        [Fact]
        public async Task ReturnsValueThatWasSetUpForQueryWithPredicateAsync()
        {
            // Arrange
            var queryProcessor = new FakeQueryProcessor();
            queryProcessor.SetupResultFor<TestQueryB>(q => q.Number == 1337, 1234);

            // Act
            var result = await queryProcessor.ExecuteAsync(new TestQueryB(1337));

            // Assert
            result.ShouldBe(1234);
        }

        [Fact]
        public async Task ReturnsComputedValueThatWasSetUpForQueryWithPredicateAsync()
        {
            // Arrange
            var queryProcessor = new FakeQueryProcessor();
            queryProcessor.SetupResultFor<TestQueryB>(q => q.Number == 1337, q => (int)q.Number);

            // Act
            var result = await queryProcessor.ExecuteAsync(new TestQueryB(1337));

            // Assert
            result.ShouldBe(1337);
        }

        [Fact]
        public async Task ReturnsDefaultValueIfQueryDoesntMatchPredicateAsync()
        {
            // Arrange
            var queryProcessor = new FakeQueryProcessor();
            queryProcessor.SetupResultFor<TestQueryB>(q => q.Number == 1337, 1234);

            // Act
            var result = await queryProcessor.ExecuteAsync(new TestQueryB(9999));

            // Assert
            result.ShouldBe(default(int));
        }

        [Fact]
        public async Task ThrowsForQueryWithThrowSetupAsync()
        {
            // Arrange
            var queryProcessor = new FakeQueryProcessor();
            queryProcessor.SetupExceptionFor<TestQueryB>(new AbandonedMutexException());

            // Act + Assert
            await Assert.ThrowsAsync<AbandonedMutexException>(async () => await queryProcessor.ExecuteAsync(new TestQueryB(1337)));
        }

        [Fact]
        public async Task ThrowsForQueryWithThrowSetupMatchingPredicateAsync()
        {
            // Arrange
            var queryProcessor = new FakeQueryProcessor();
            queryProcessor.SetupExceptionFor<TestQueryB>(q => q.Number == 1337, new AbandonedMutexException());

            // Act + Assert
            await Assert.ThrowsAsync<AbandonedMutexException>(async () => await queryProcessor.ExecuteAsync(new TestQueryB(1337)));
        }

        [Fact]
        public async Task DoesNotThrowIfQueryDoesntMatchPredicateAsync()
        {
            // Arrange
            var queryProcessor = new FakeQueryProcessor();
            queryProcessor.SetupExceptionFor<TestQueryB>(q => q.Number == 1337, new AbandonedMutexException());

            // Act
            var result = await queryProcessor.ExecuteAsync(new TestQueryB(9999));

            // Assert
            result.ShouldBe(default(int));
        }
    }
}
