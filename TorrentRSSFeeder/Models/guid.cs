using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace TorrentRSSFeeder.Models
{
    public class guid
    {
        [XmlText]
        public string value { get; set; }
        [XmlAttribute]

        public bool isPermaLink = false;
    }
}