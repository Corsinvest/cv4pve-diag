using System.ComponentModel;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api
{
    /// <summary>
    /// Settings
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Store
        /// </summary>
        /// <returns></returns>
        public SettingsThresholdTimeSeries Storage { get; set; } = new SettingsThresholdTimeSeries();

        /// <summary>
        /// Node
        /// </summary>
        /// <returns></returns>
        public SettingsThresholdHost Node { get; set; } = new SettingsThresholdHost();

        /// <summary>
        /// Qemu
        /// </summary>
        /// <returns></returns>
        public SettingsThresholdHost Qemu { get; set; } = new SettingsThresholdHost();

        /// <summary>
        /// Lxc
        /// </summary>
        /// <returns></returns>
        public SettingsThresholdHost Lxc { get; set; } = new SettingsThresholdHost();

        /// <summary>
        /// Threshold
        /// </summary>
        [DisplayName("Ssd SsdWearout")]
        public SettingsThresholdPercentual SsdWearoutThreshold { get; set; } = new SettingsThresholdPercentual();
    }
}