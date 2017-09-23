using Generic.LightDataTable.Helper;
using System;

namespace Generic.LightDataTable
{
    public class RoundingSettings
    {
        private RoundingConvention? _roundingConvention;
        public int RowDecimalRoundTotal { get; set; }

        public RoundingConvention? RoundingConventionMethod
        {
            get
            {
                return _roundingConvention;
            }
            set
            {
                _roundingConvention = value;
            }
        }

        public RoundingSettings()
        {
            RowDecimalRoundTotal = 5;// Default Value
        }

        public decimal Round(decimal value)
        {
            bool isNegative = (RoundingConventionMethod != null && RoundingConventionMethod != RoundingConvention.Normal) && value < 0;
            if (RoundingConventionMethod != null && isNegative)
                value = Math.Abs(value); // its very importend to have a positive number when using floor/Ceiling
            if (RoundingConventionMethod == null || RoundingConventionMethod == RoundingConvention.Normal)
                value = Math.Round(value, this.RowDecimalRoundTotal);
            else if (RoundingConventionMethod == RoundingConvention.RoundUpp)
            {
                var adjustment = (decimal)Math.Pow(10, RowDecimalRoundTotal);
                value = (Math.Ceiling(value * adjustment) / adjustment);
            }
            else if (RoundingConventionMethod == RoundingConvention.RoundDown)
            {
                var adjustment = (decimal)Math.Pow(10, RowDecimalRoundTotal);
                value = (Math.Floor(value * adjustment) / adjustment);
            }
            else if (RoundingConventionMethod == RoundingConvention.None)
            {
                var adjustment = (decimal)Math.Pow(10, RowDecimalRoundTotal);
                value = (Math.Truncate(value * adjustment) / adjustment);
            }
            if (isNegative)
                value = -value;

            return value;
        }

        public double Round(double value)
        {
            bool isNegative = (RoundingConventionMethod != null && RoundingConventionMethod != RoundingConvention.Normal) && value < 0;
            if (RoundingConventionMethod != null && isNegative)
                value = Math.Abs(value); // its very importend to have a positive number when using floor/Ceiling
            if (RoundingConventionMethod == null || RoundingConventionMethod == RoundingConvention.Normal)
                value = Math.Round(value, this.RowDecimalRoundTotal);
            else if (RoundingConventionMethod == RoundingConvention.RoundUpp)
            {
                var adjustment = Math.Pow(10, RowDecimalRoundTotal);
                value = (Math.Ceiling(value * adjustment) / adjustment);
            }
            else if (RoundingConventionMethod == RoundingConvention.RoundDown)
            {
                var adjustment = Math.Pow(10, RowDecimalRoundTotal);
                value = (Math.Floor(value * adjustment) / adjustment);
            }
            else if (RoundingConventionMethod == RoundingConvention.None)
            {
                var adjustment = Math.Pow(10, RowDecimalRoundTotal);
                value = (Math.Truncate(value * adjustment) / adjustment);
            }

            if (isNegative)
                value = -value;

            return value;
        }
    }
}
