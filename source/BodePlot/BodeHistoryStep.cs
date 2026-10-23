using System;
using System.Globalization;
using System.Drawing;
using SMath.Controls;

namespace EngineeringTools
{
    public sealed class BodeHistoryStep : HistoryStepEvaluable
    {
        private readonly BodeSettings settings;
        private readonly string signature;
        private readonly Size size;
        public BodeHistoryStep(BodeRegion region) : base(region)
        {
            settings = (region.Settings ?? new BodeSettings()).Clone();
            size = region.Size;
            signature = string.Join("|", new[]
            {
                settings.Series, settings.HiddenSeries, settings.Minimum.ToString("R", CultureInfo.InvariantCulture), settings.Maximum.ToString("R", CultureInfo.InvariantCulture), settings.Samples.ToString(),
                settings.LogFrequency.ToString(), settings.Decibels.ToString(), settings.RadiansFrequency.ToString(), settings.RadiansPhase.ToString(), settings.UnwrapPhase.ToString(),
                settings.AutoMagnitude.ToString(), settings.AutoPhase.ToString(), settings.MagnitudeMinimum.ToString("R", CultureInfo.InvariantCulture), settings.MagnitudeMaximum.ToString("R", CultureInfo.InvariantCulture),
                settings.PhaseMinimum.ToString("R", CultureInfo.InvariantCulture), settings.PhaseMaximum.ToString("R", CultureInfo.InvariantCulture)
            });
        }
        public override void Apply(RegionBase region)
        {
            ((BodeRegion)region).Settings = settings.Clone();
            base.Apply(region);
            ((BodeRegion)region).SetPlotSize(size);
            ((BodeRegion)region).RequestEvaluation(true);
        }
        public override bool Equals(object other)
        {
            BodeHistoryStep step = other as BodeHistoryStep;
            return step != null && signature == step.signature && size == step.size && base.Equals(other);
        }
        public override int GetHashCode() { return base.GetHashCode() ^ signature.GetHashCode() ^ size.GetHashCode(); }
    }
}
