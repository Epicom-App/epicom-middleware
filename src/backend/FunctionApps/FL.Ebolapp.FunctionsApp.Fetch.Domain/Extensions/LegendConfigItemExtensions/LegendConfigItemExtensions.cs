using FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Extensions.LegendConfigItemExtensions
{
    public static class LegendConfigItemExtensions
    {
        public static Severity ToSeverity(this LegendConfigItem entity)
            => new Severity
            {
                Value = entity.Severity,
                LowerBoundary = entity.LowerBoundary,
                UpperBoundary =entity.UpperBoundary 
            };
    }
}