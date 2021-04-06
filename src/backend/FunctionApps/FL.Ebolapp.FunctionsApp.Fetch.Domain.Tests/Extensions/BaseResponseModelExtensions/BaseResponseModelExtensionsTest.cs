using System.Collections.Generic;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.Extensions.BaseResponseModelExtensions;
using FL.Ebolapp.FunctionsApp.Fetch.Domain.ResponseModels;
using Xunit;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Tests.Extensions.BaseResponseModelExtensions
{
    public class BaseResponseModelExtensionsTest
    {
        private static readonly List<Severity> Severities = new List<Severity>
        {
            new Severity
            {
                Value = 0,
                LowerBoundary = null,
                UpperBoundary = 0,
            },
            new Severity
            {
                Value = 1,
                LowerBoundary = 0,
                UpperBoundary = 5,
            },
            new Severity
            {
                Value = 2,
                LowerBoundary = 5,
                UpperBoundary = 25,
            },
            new Severity
            {
                Value = 3,
                LowerBoundary = 25,
                UpperBoundary = null,
            },
        };

        [Theory]
        [InlineData(-5, 0)]
        [InlineData(0, 0)]

        [InlineData(1, 1)]
        [InlineData(3, 1)]
        [InlineData(5, 1)]

        [InlineData(6, 2)]
        [InlineData(16, 2)]
        [InlineData(25, 2)]

        [InlineData(26, 3)]
        [InlineData(30, 3)]
        public void GivenData_ShouldReturnCorrect(double cases, int expected)
        {
            // arrange
            var sut = new DistrictResponseModel
            {
                Cases7Per100K = cases
            };

            // act
            var actual = sut.CalculateSeverity(Severities);

            // assert
            Assert.Equal(expected, actual);
        }
    }
}