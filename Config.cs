using System.Collections.Generic;

namespace owfsmq
{
    public class Config
    {
        /// <summary>
        /// IP or hostname of MQTT host. E.g.  "192.168.0.22" or "mosquitto".
        /// </summary>
        public string MqttHost { get; set; }
        /// <summary>
        /// TCP port of the MQTT host. 1883 is a typical.
        /// </summary>
        public int MqttPort { get; set; }
        /// <summary>
        /// MQTT user name (optional)   
        /// </summary>
        public string MqttUserName { get; internal set; }
        /// <summary>
        /// QoS for MQTT messages 0..2. Default 0.
        /// </summary>
        public int MqttQoS { get; set; }
        /// <summary>
        /// MQTT broker password (optional).
        /// </summary>
        public string MqttPassword { get; internal set; }
        /// <summary>
        /// Host running owhhttp.  E.g. "http://onewire:2121"
        /// </summary>
        public string OwHttpHost { get; set; }
        /// <summary>
        /// List of 1-wire devices to poll on 1-wire and broadcast on mqtt.
        /// </summary>
        public List<DeviceConfig> Devices { get; set; }
        /// <summary>
        /// Polling interval in seconds.
        /// </summary>
        public int IntervalSeconds { get; set; }        
    }

    public class DeviceConfig
    {
        /// <summary>
        /// Id of the 1wire device. For example 28.1234000000
        /// </summary>
        public string DeviceId { get; set; }
        /// <summary>
        /// Measurement to read from the device, e.g. "temperature"
        /// </summary>
        public string Measurement { get; set; }
        /// <summary>
        /// MQTT Topic to post this value to.  E.g. "onewire/kitchen_temperature"
        /// </summary>
        public string Topic { get; set; }
    }

}
