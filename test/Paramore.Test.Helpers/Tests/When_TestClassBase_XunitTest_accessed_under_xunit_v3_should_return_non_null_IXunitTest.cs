using Paramore.Test.Helpers.Base;
using Shouldly;
using Xunit;
using Xunit.v3;

namespace Paramore.Test.Helpers.Tests
{
    public class When_TestClassBase_XunitTest_accessed_under_xunit_v3_should_return_non_null_IXunitTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public When_TestClassBase_XunitTest_accessed_under_xunit_v3_should_return_non_null_IXunitTest(
            ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        // A concrete TestClassBase<T> so we can read its XunitTest / TestQualifiedName surface.
        private sealed class SampleTestClass : TestClassBase<SampleTestClass>
        {
            public SampleTestClass(ITestOutputHelper testOutputHelper)
                : base(testOutputHelper)
            {
            }
        }

        [Fact]
        public void XunitTest_should_be_non_null_IXunitTest_and_TestQualifiedName_should_be_the_running_test_method()
        {
            // Arrange — a TestClassBase<T> instantiated inside a running xunit.v3 test,
            // so TestContext.Current.Test is populated with this very test's metadata.
            var sut = new SampleTestClass(_testOutputHelper);

            // Act
            IXunitTest? xunitTest = sut.XunitTest;

            // Assert — option (a): the v3 TestContext path yields a non-null IXunitTest,
            // and TestQualifiedName reflects the running test method (not the type-name fallback).
            xunitTest.ShouldNotBeNull();
            sut.TestQualifiedName.ShouldContain(
                nameof(XunitTest_should_be_non_null_IXunitTest_and_TestQualifiedName_should_be_the_running_test_method));
        }
    }
}
