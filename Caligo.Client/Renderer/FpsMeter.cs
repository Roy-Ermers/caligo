using System.Diagnostics.Metrics;

namespace Caligo.Client.Renderer;

public static class FpsMeter
{
    private static readonly Meter _meter = new("Caligo.Client.Renderer.FpsMeter");

    public static readonly Gauge<double> FpsGauge =
        _meter.CreateGauge<double>("fps", "frames per second", "Current frames per second");
}