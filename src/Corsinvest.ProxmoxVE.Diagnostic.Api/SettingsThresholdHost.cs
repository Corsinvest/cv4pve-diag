using System.ComponentModel;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api
{
    /// <summary>
    /// Settings Threshold Host
    /// </summary>
    public class SettingsThresholdHost
    {
        /// <summary>
        /// TimeSeries
        /// </summary>
        /// <value></value>
        [DisplayName("Time Series")]
        public SettingsTimeSeriesType TimeSeries { get; set; } = SettingsTimeSeriesType.Day;

        /// <summary>
        /// Cpu
        /// </summary>
        /// <returns></returns>
        public SettingsThresholdPercentual Cpu { get; set; } = new SettingsThresholdPercentual();

        /// <summary>
        /// Memory
        /// </summary>
        /// <returns></returns>
        public SettingsThresholdPercentual Memory { get; set; } = new SettingsThresholdPercentual();

        /// <summary>
        /// Network
        /// </summary>
        public SettingsThreshold<double> Network { get; set; } = new SettingsThreshold<double>();
    }
}