using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api
{
    /// <summary>
    /// Settings Threshold TimeSeries
    /// </summary>
    public class SettingsThresholdTimeSeries
    {
        /// <summary>
        /// TimeSeries
        /// </summary>
        [DisplayName("Time Series")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SettingsTimeSeriesType TimeSeries { get; set; } = SettingsTimeSeriesType.Day;

        /// <summary>
        /// Threshold
        /// </summary>
        [DisplayName("Threshold")]
        public SettingsThresholdPercentual Threshold { get; set; } = new SettingsThresholdPercentual();
    }
}