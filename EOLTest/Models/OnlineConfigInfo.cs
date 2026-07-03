
// 用途：对应配置文件 <info> 根节点及其子元素的序列化模型

using System.Collections.Generic;
using System.Xml.Serialization;

namespace EOLTest.Models
{
    /// <summary>
    /// 在线配置 XML 的根节点
    /// </summary>
    [XmlRoot("info")]
    public class OnlineConfigInfo
    {
        [XmlElement("result")]
        public string Result { get; set; }

        [XmlElement("vin")]
        public string Vin { get; set; }

        [XmlElement("carType")]
        public string CarType { get; set; }

        [XmlElement("configVersion")]
        public string ConfigVersion { get; set; }

        [XmlElement("pns")]
        public string Pns { get; set; }

        [XmlElement("vsn")]
        public string Vsn { get; set; }

        [XmlElement("rpo")]
        public string Rpo { get; set; }

        [XmlElement("ecu")]
        public List<EcuConfigItem> EcuList { get; set; }
    }

    /// <summary>
    /// 配置文件中的 <ecu> 节点
    /// </summary>
    public class EcuConfigItem
    {
        [XmlElement("ecuName")]
        public string EcuName { get; set; }

        [XmlElement("partNumber")]
        public string PartNumber { get; set; }

        [XmlElement("codes")]
        public EcuCodes Codes { get; set; }
    }

    /// <summary>
    /// <codes> 节点内容
    /// </summary>
    public class EcuCodes
    {
        [XmlElement("data")]
        public string Data { get; set; }

        [XmlElement("did")]
        public string Did { get; set; }

        [XmlElement("serviceID")]
        public string ServiceID { get; set; }
    }
}
