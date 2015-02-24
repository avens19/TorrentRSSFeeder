using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace TorrentRSSFeeder.Models
{
    public class rss
    {
        [XmlAttribute("version")]
        public string version = "2.0";
        public channel channel { get; set; }
    }
}