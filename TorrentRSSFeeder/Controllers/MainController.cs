﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Xml.Serialization;
using System.Xml.XPath;
using HtmlAgilityPack;
using TorrentRSSFeeder.Models;

namespace TorrentRSSFeeder.Controllers
{
    public class MainController : ApiController
    {
        [HttpGet]
        [Route("tpbtv")]
        public async Task<HttpResponseMessage> ThePirateBayTV()
        {
            string url = ConfigurationManager.AppSettings["PirateBayTVURL"];
            string xPath = ConfigurationManager.AppSettings["PirateBayXpath"]; 

            HttpWebRequest request = WebRequest.CreateHttp(url);

            request.Accept = "application/xml";

            List<item> items = new List<item>();

            using (var stream = (await request.GetResponseAsync()).GetResponseStream())
            {
                if (stream == null)
                    return null;

                HtmlDocument doc = new HtmlDocument();
                doc.Load(stream);
                var nodes = doc.DocumentNode.SelectNodes(xPath);
                foreach(HtmlNode td in nodes)
                {
                    var description = td.Element("div").Element("a").InnerText;
                    var magnet = td.Element("a").GetAttributeValue("href", null);
                    var time = DateTime.UtcNow;
                    var timeString = HttpUtility.HtmlDecode(td.Element("font").InnerText);
                    timeString = timeString.Replace("Uploaded ", "");
                    timeString = timeString.Substring(0, timeString.IndexOf(",", StringComparison.CurrentCultureIgnoreCase));
                    bool found = false;
                    var index = timeString.IndexOf("mins", StringComparison.CurrentCultureIgnoreCase);
                    if (index > -1)
                    {
                        var mins = int.Parse(timeString.Substring(0, index - 1));
                        time = time.AddMinutes(mins*-1);
                        found = true;
                    }
                    index = timeString.IndexOf("Today", StringComparison.CurrentCultureIgnoreCase);
                    if (!found && index > -1)
                    {
                        var timeGMTString = timeString.Substring(6);
                        var hours = int.Parse(timeGMTString.Substring(0, 2));
                        var mins = int.Parse(timeGMTString.Substring(3));
                        TimeZoneInfo gmtZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
                        DateTime timeGMT = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, gmtZone);
                        timeGMT = timeGMT.AddHours(timeGMT.Hour*-1);
                        timeGMT = timeGMT.AddMinutes(timeGMT.Minute*-1);
                        timeGMT = timeGMT.AddSeconds(timeGMT.Second*-1);
                        timeGMT = timeGMT.AddMilliseconds(timeGMT.Millisecond * -1);
                        timeGMT = timeGMT.AddHours(hours);
                        timeGMT = timeGMT.AddMinutes(mins);
                        time = TimeZoneInfo.ConvertTimeToUtc(timeGMT, gmtZone);
                        found = true;
                    }
                    index = timeString.IndexOf("Y-day", StringComparison.CurrentCultureIgnoreCase);
                    if (!found && index > -1)
                    {
                        var timeGMTString = timeString.Substring(6);
                        var hours = int.Parse(timeGMTString.Substring(0, 2));
                        var mins = int.Parse(timeGMTString.Substring(3));
                        TimeZoneInfo gmtZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
                        DateTime timeGMT = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, gmtZone);
                        timeGMT = timeGMT.AddDays(-1);
                        timeGMT = timeGMT.AddHours(timeGMT.Hour * -1);
                        timeGMT = timeGMT.AddMinutes(timeGMT.Minute * -1);
                        timeGMT = timeGMT.AddSeconds(timeGMT.Second * -1);
                        timeGMT = timeGMT.AddMilliseconds(timeGMT.Millisecond * -1);
                        timeGMT = timeGMT.AddHours(hours);
                        timeGMT = timeGMT.AddMinutes(mins);
                        time = TimeZoneInfo.ConvertTimeToUtc(timeGMT, gmtZone);
                        found = true;
                    }
                    if (!found)
                    {
                        TimeZoneInfo gmtZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
                        DateTime timeGMT = DateTime.Parse(DateTime.UtcNow.Year + "-" + timeString);
                        time = TimeZoneInfo.ConvertTimeToUtc(timeGMT, gmtZone);
                    }
                    items.Add(new item
                    {
                        description = description,
                        link = magnet,
                        pubDate = time.ToString("r"),
                        title = description
                    });
                }
            }

            MemoryStream ms = new MemoryStream();
            using (StreamWriter sw = new StreamWriter(ms))
            {
                new XmlSerializer(typeof (rss)).Serialize(sw,
                    new rss
                    {
                        channel = new channel
                        {
                            items = items.ToArray()
                        }
                    });

                ms.Position = 0;

                using (StreamReader sr = new StreamReader(ms))
                {
                    return new HttpResponseMessage
                    {
                        Content = new StringContent(sr.ReadToEnd()),
                        StatusCode = HttpStatusCode.OK
                    };
                }
            }


        }
    }
}
