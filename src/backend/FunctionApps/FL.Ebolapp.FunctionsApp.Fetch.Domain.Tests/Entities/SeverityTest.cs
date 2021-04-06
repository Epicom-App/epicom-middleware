using FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities;
using Xunit;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Tests.Entities
{
    public class SeverityTest
    {
        [Theory]
        [InlineData(null, 0, -5, true)]
        [InlineData(null, 0, 0, true)]
        [InlineData(null, 0, 1, false)]

        [InlineData(1, 5, 1, false)]
        [InlineData(1, 5, 1.01, true)]
        [InlineData(1, 5, 5, true)]
        [InlineData(1, 5, 3, true)]
        [InlineData(1, 5, 0, false)]
        [InlineData(1, 5, 6, false)]

        [InlineData(26, null, 27, true)]
        [InlineData(26, null, 26, false)]
        [InlineData(26, null, 25, false)]
        [InlineData(26, null, 0, false)]
        public void GivenInput_MatchesShouldReturn(double? lowerBoundary, double? upperBoundary, double testValue, bool expected)
        {
            // arrange
            var sut = new Severity
            {
                LowerBoundary = lowerBoundary,
                UpperBoundary = upperBoundary,
                Value = -1,
            };

            // act
            var actual = sut.Matches(testValue);

            // assert
            Assert.Equal(expected, actual);
        }
    }
}
