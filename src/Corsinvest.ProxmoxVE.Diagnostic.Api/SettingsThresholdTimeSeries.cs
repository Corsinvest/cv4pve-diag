using System.ComponentModel;

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
        public SettingsTimeSeriesType TimeSeries { get; set; } = SettingsTimeSeriesType.Day;

        /// <summary>
        /// Threshold
        /// </summary>
        [DisplayName("Threshold")]
        public SettingsThresholdPercentual Threshold { get; set; } = new SettingsThresholdPercentual();
    }
}