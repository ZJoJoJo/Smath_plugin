using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SMath.Manager;
using SMath.Math;
using SMath.Math.Numeric;

namespace EngineeringTools
{
    public sealed class BodeSettings
    {
        public string Series = "H.0;H.1(s);H.2(s);T.OL(s)";
        public string HiddenSeries = "";
        public double Minimum = 10;
        public double Maximum = 1000000;
        public int Samples = 501;
        public bool LogFrequency = true;
        public bool Decibels = true;
        public bool RadiansFrequency;
        public bool RadiansPhase;
        public bool UnwrapPhase = true;
        public bool AutoMagnitude = true;
        public bool AutoPhase = true;
        public double MagnitudeMinimum = -80;
        public double MagnitudeMaximum = 80;
        public double PhaseMinimum = -270;
        public double PhaseMaximum = 90;

        public BodeSettings Clone() { return (BodeSettings)MemberwiseClone(); }

        public bool IsSeriesVisible(string label)
        {
            return !(HiddenSeries ?? "").Split(';').Contains(label, StringComparer.Ordinal);
        }

        public void SetSeriesVisible(string label, bool visible)
        {
            var hidden = new HashSet<string>((HiddenSeries ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
            if (visible) hidden.Remove(label); else hidden.Add(label);
            HiddenSeries = string.Join(";", hidden.OrderBy(value => value, StringComparer.Ordinal));
        }

        public void Validate()
        {
            if (!Finite(Minimum) || !Finite(Maximum) || Minimum <= 0 || Maximum <= Minimum)
                throw new ArgumentException("频率范围必须满足 0 < 最小值 < 最大值。");
            if (Samples < 51 || Samples > 2001)
                throw new ArgumentException("采样点数应在 51 到 2001 之间。");
            if (!AutoMagnitude && (!Finite(MagnitudeMinimum) || !Finite(MagnitudeMaximum) || MagnitudeMaximum <= MagnitudeMinimum))
                throw new ArgumentException("幅值轴最大值必须大于最小值。");
            if (!AutoPhase && (!Finite(PhaseMinimum) || !Finite(PhaseMaximum) || PhaseMaximum <= PhaseMinimum))
                throw new ArgumentException("相位轴最大值必须大于最小值。");
            if (Specs().Count == 0 || Specs().Count > 8)
                throw new ArgumentException("请输入 1 至 8 条曲线，以分号分隔。");
        }

        public List<SeriesSpec> Specs()
        {
            return (Series ?? "").Split(new[] { ';', '；' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(text => SeriesSpec.Parse(text.Trim())).ToList();
        }

        public static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }

    public sealed class SeriesSpec
    {
        public string Name;
        public bool Function;
        public string Label { get { return Name + (Function ? "(s)" : ""); } }

        public static SeriesSpec Parse(string text)
        {
            Match match = Regex.Match(text, @"^([^\s;；()]+)(\(\s*s\s*\))?$", RegexOptions.CultureInvariant);
            if (!match.Success)
                throw new ArgumentException("曲线格式应为 H.0 或 H.1(s)，多个曲线用分号分隔。");
            return new SeriesSpec { Name = match.Groups[1].Value, Function = match.Groups[2].Success };
        }
    }

    public struct Response
    {
        public double Magnitude;
        public double Phase;
        public Response(double magnitude, double phase) { Magnitude = magnitude; Phase = phase; }
    }

    public sealed class BodeSeries
    {
        public SeriesSpec Spec;
        public double[] Magnitudes;
        public double[] Phases;
    }

    public sealed class BodeData
    {
        public double[] Frequencies;
        public readonly List<BodeSeries> Series = new List<BodeSeries>();

        public static double[] FrequencyGrid(BodeSettings settings)
        {
            settings.Validate();
            double[] frequencies = new double[settings.Samples];
            double lower = settings.LogFrequency ? Math.Log(settings.Minimum) : settings.Minimum;
            double upper = settings.LogFrequency ? Math.Log(settings.Maximum) : settings.Maximum;
            for (int index = 0; index < frequencies.Length; index++)
            {
                double value = lower + (upper - lower) * index / (frequencies.Length - 1);
                frequencies[index] = settings.LogFrequency ? Math.Exp(value) : value;
            }
            frequencies[0] = settings.Minimum;
            frequencies[frequencies.Length - 1] = settings.Maximum;
            return frequencies;
        }

        public static BodeData Sample(BodeSettings settings, Func<SeriesSpec, double, Response> evaluate)
        {
            BodeData data = new BodeData { Frequencies = FrequencyGrid(settings) };
            foreach (SeriesSpec spec in settings.Specs())
            {
                BodeSeries series = new BodeSeries { Spec = spec, Magnitudes = new double[settings.Samples], Phases = new double[settings.Samples] };
                double previousPhase = double.NaN;
                for (int index = 0; index < settings.Samples; index++)
                {
                    double omega = data.Frequencies[index] * (settings.RadiansFrequency ? 1 : 2 * Math.PI);
                    Response response;
                    try { response = evaluate(spec, omega); }
                    catch (Exception exception)
                    {
                        throw new InvalidOperationException(spec.Label + " 在频率 " + data.Frequencies[index].ToString("G5", CultureInfo.InvariantCulture) + " 处计算失败：" + exception.Message, exception);
                    }
                    double phase = response.Phase;
                    if (settings.UnwrapPhase && BodeSettings.Finite(phase) && BodeSettings.Finite(previousPhase))
                        phase += 2 * Math.PI * Math.Round((previousPhase - phase) / (2 * Math.PI));
                    previousPhase = phase;
                    series.Magnitudes[index] = response.Magnitude >= 0 ? (settings.Decibels ? 20 * Math.Log10(response.Magnitude) : response.Magnitude) : double.NaN;
                    series.Phases[index] = phase * (settings.RadiansPhase ? 1 : 180 / Math.PI);
                }
                data.Series.Add(series);
            }
            return data;
        }

        public static double Interpolate(double[] frequencies, double[] values, double frequency, bool logarithmic)
        {
            int index = Array.BinarySearch(frequencies, frequency);
            if (index >= 0) return values[index];
            index = ~index;
            if (index <= 0) return values[0];
            if (index >= frequencies.Length) return values[values.Length - 1];
            double left = logarithmic ? Math.Log(frequencies[index - 1]) : frequencies[index - 1];
            double right = logarithmic ? Math.Log(frequencies[index]) : frequencies[index];
            double position = logarithmic ? Math.Log(frequency) : frequency;
            return values[index - 1] + (values[index] - values[index - 1]) * (position - left) / (right - left);
        }
    }

    public static class SMathResponse
    {
        public static Response Evaluate(Store store, SeriesSpec spec, double omega)
        {
            Term[] terms;
            if (spec.Function)
            {
                List<Term> expression = new List<Term>(new TComplex(0, omega).ToTerms());
                expression.Add(new Term(spec.Name, TermType.Function, 1));
                terms = expression.ToArray();
            }
            else terms = new[] { new Term(spec.Name, TermType.Operand, 0) };
            TNumber value = Computation.NumericCalculation(Entry.Create(terms), store);
            if (value.obj.HasUnits)
                throw new InvalidOperationException("当前波特图需要无量纲传递函数，请先归一化单位。");
            return new Response(value.Abs().obj.ToDouble(), value.Arg().obj.ToDouble());
        }
    }
}
