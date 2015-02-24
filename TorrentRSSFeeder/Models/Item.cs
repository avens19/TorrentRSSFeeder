using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace TorrentRSSFeeder.Models
{
    public class item
    {
        public string title { get; set; }
        public string link { get; set; }
        public string description { get; set; }
        public string pubDate { get; set; }

        public guid guid = new guid
        {
            value = Guid.NewGuid().ToString()
        };
    }
}