namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities
{
    public class Severity
    {
        public int Value { get; set; }
        public double? LowerBoundary { get; set; }
        public double? UpperBoundary { get; set; }

        public bool Matches(double value)
        {
            if (LowerBoundary < value && value <= UpperBoundary)
            {
                return true;
            }

            if (LowerBoundary == null && value <= UpperBoundary)
            {
                return true;
            }

            if (UpperBoundary == null && LowerBoundary < value)
            {
                return true;
            }

            return false;
        }
    }
}