using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Xml.Serialization;
using System.Xml.XPath;
using HtmlAgilityPack;
using TorrentRSSFeeder.Models;
using TorrentRSSFeeder.Helpers;

namespace TorrentRSSFeeder.Controllers
{
    public class MainController : ApiController
    {
        [HttpGet]
        [Route("tpbtv")]
        public async Task<HttpResponseMessage> ThePirateBayTV()
        {
            return await ParseWebsite("PirateBayTVURL", "PirateBayXpath", (td) =>
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
                    time = time.AddMinutes(mins * -1);
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
                    timeGMT = timeGMT.AddHours(timeGMT.Hour * -1);
                    timeGMT = timeGMT.AddMinutes(timeGMT.Minute * -1);
                    timeGMT = timeGMT.AddSeconds(timeGMT.Second * -1);
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
                return new item
                {
                    description = description,
                    link = magnet,
                    pubDate = time.ToString("r"),
                    title = description
                };
            });
        }

        [HttpGet]
        [Route("eztv")]
        public async Task<HttpResponseMessage> EZTV()
        {
            return await ParseWebsite("EZTVURL", "EZTVXpath", (tr) =>
            {
                var description = tr.Elements("td").ToArray()[1].Element("a").InnerText;
                var magnet = tr.Elements("td").ToArray()[2].Element("a").GetAttributeValue("href", null);
                var time = DateTime.UtcNow;
                var timeString = tr.Elements("td").ToArray()[3].InnerText.Trim();
                var timeStrings = timeString.Split(' ');
                foreach (var ts in timeStrings)
                {
                    if (ts.IndexOf("d", StringComparison.CurrentCultureIgnoreCase) > -1)
                    {
                        time = time.AddDays(int.Parse(ts.Substring(0, ts.Length - 1)) * -1);
                    }
                    else if (ts.IndexOf("h", StringComparison.CurrentCultureIgnoreCase) > -1)
                    {
                        time = time.AddHours(int.Parse(ts.Substring(0, ts.Length - 1)) * -1);
                    }
                    else if (ts.IndexOf("m", StringComparison.CurrentCultureIgnoreCase) > -1)
                    {
                        time = time.AddMinutes(int.Parse(ts.Substring(0, ts.Length - 1)) * -1);
                    }
                    else if (ts.IndexOf("s", StringComparison.CurrentCultureIgnoreCase) > -1)
                    {
                        time = time.AddSeconds(int.Parse(ts.Substring(0, ts.Length - 1)) * -1);
                    }
                }
                return new item
                {
                    description = description,
                    link = magnet,
                    pubDate = time.ToString("r"),
                    title = description
                };
            });
        }
        
        private async Task<HttpResponseMessage> ParseWebsite(string urlSetting, string xPathSetting, Func<HtmlNode, item> handleItem)
        {
            string url = ConfigurationManager.AppSettings[urlSetting];
            string xPath = ConfigurationManager.AppSettings[xPathSetting];

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
                foreach (HtmlNode node in nodes)
                {
                    items.Add(handleItem(node));
                }
            }

            return new HttpResponseMessage
            {
                Content = new StringContent(new rss
                {
                    channel = new channel
                    {
                        items = items.ToArray()
                    }
                }.SerializeObject<rss>(),
                Encoding.UTF8,
                "application/rss+xml"),
                StatusCode = HttpStatusCode.OK
            };
        }
    }
}
