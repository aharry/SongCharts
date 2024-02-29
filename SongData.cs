using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongCharts
{
    internal class SongData
    {
        public string Uuid { get; set; } = string.Empty;
        public string ExternalStem { get; set; } = string.Empty;
        public string AudioFileName { get; set; } = string.Empty;
        public string SongName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string SongFormatVersion { get; set; } = string.Empty;
    }

    internal class Tonal
    {
        public string RealKey { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public int PitchTranspose { get; set; }
        public double EstimatedTuning { get; set; }
    }
    internal class Marker
    {
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public string MarkerText { get; set; } = string.Empty;
        public string MarkerColor { get; set; } = string.Empty;
        public int NumBars { get; set; }
    }

    internal class Beats
    {
        public double AvgBpm { get; set; }
        public double Bpm { get; set; }
        public double AudioLength { get; set; }
    }

    internal class BarBeat
    {
        public double Time { get; set; }
        public int Bar { get; set; }
        public int Beat { get; set; }
    }

    internal class Bar
    {
        public int SmBar { get; set; }
        public string? Chords { get; set; }

    }
}
