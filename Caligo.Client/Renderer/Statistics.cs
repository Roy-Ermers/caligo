using System.Diagnostics.Metrics;

namespace Caligo.Client.Renderer;

public static class Statistics
{
    private static readonly Meter _meter = new("Caligo.Client.Renderer.Statistics");

    public static readonly Gauge<double> FpsGauge =
        _meter.CreateGauge<double>("fps", "frames per second", "Current frames per second");

    public static readonly Gauge<double> ChunkMesherSpeedGauge =
        _meter.CreateGauge<double>("chunk_mesher_speed", "chunks per second", "Current chunk mesher speed");
}