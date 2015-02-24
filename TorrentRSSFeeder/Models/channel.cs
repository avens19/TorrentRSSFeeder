using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace TorrentRSSFeeder.Models
{
    public class channel
    {
        public string title = "TPB TV";
        public string link = "http://localhost:5050/tpbtv";
        public int ttl = 30;
        public string description = "TPB TV";

        [XmlElement("item")]
        public item[] items { get; set; }
    }
}