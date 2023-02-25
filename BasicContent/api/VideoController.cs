#if NETCOREAPP // Oqtane
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
#else // DNN
using System.Web.Http;
using DotNetNuke.Web.Api;
#endif

using System.Linq;
using System.IO;
using System.Xml;
using ToSic.Razor.Blade;
using Dynlist = System.Collections.Generic.IEnumerable<dynamic>;

[AllowAnonymous]
public class VideoController : Custom.Hybrid.Api12
{
    public const string AtomNsCode = "atom";
    public const string AtomNamespace = "http://www.w3.org/2005/Atom";
    public const string EmptyRssDocument = "<?xml version='1.0' encoding='utf-8'?>"
        + "<rss version='2.0' xmlns:" + AtomNsCode + "='" + AtomNamespace + "'></rss>";
    public const string ErrDetailPage = "Error: 'DetailPage' app setting is missing. Please configure to get this RSS feed to work.";

    [HttpGet]
    public dynamic Rss()
    {
        // 1. Prepare
        // 1.1 Figure out what page will show video details based on settings
        // If the settings are configured, it's something like "page:27"
        var videoDetailPageId = Text.Has(Settings.DetailPage)
            ? int.Parse((Settings.Get("DetailPage", convertLinks: false)).Split(':')[1])
            : 0; // when 'DetailPage' app setting is missing.

        // 1.2 This will be null or a message. To be used instead of links
        var linkErrMessage = (videoDetailPageId == 0) ? ErrDetailPage : null;

        // 2. Build main XML document
        // 2.1 Create the XmlDocument and get root node
        var rssDoc = new XmlDocument();
        rssDoc.PreserveWhitespace = true;
        rssDoc.LoadXml(EmptyRssDocument);
        var root = rssDoc.DocumentElement;

        // 2.1 Add a warning to the XML in case we don't know what page to use for links
        // we use a custom xml namespace "warning" to not break the RSS standard
        if(videoDetailPageId == 0) {
            var warningTag = AddNamespaceTag(root, "warning", "warning", "http://warning");
            warningTag.InnerText = "Links for the video details cannot work yet, because the App isn't fully configured. You must set a DetailPage in the app settings.";
        }

        // 3. Create Channel
        // 3.1 Create <channel> node and set important values
        var channel = AddTag(root, "channel");
        var videosPageId = 21;
        AddTag(channel, "title", Resources.VideosTitle);
        AddTag(channel, "link", linkErrMessage ?? Link.To(pageId: videosPageId, type: "full"));
        AddTag(channel, "description", Resources.RssDescription);

        // 3.2 Create the <atom> tag with all the attributes. It needs to have the namespace "atom" for valid RSS
        var atom = AddNamespaceTag(channel, AtomNsCode, "link", AtomNamespace);
        AddAttribute(atom, "rel", "self");
        AddAttribute(atom, "type", "application/rss+xml");
        AddAttribute(atom, "href", Link.To(api: "api/Videos/Rss", type: "full"));

        // 3.3 Add all the videos from the query to this channel
        foreach(var video in AsList(App.Query["VideoList"]["AllVideos"])) {
            var itemNode = AddTag(channel, "item");
            AddTag(itemNode, "title", video.Title);
            AddTag(itemNode, "link", linkErrMessage ?? Link.To(pageId: videoDetailPageId, parameters: "Episode=" + video.Slug, type: "full"));
            AddTag(itemNode, "description", video.Summary); // TODO: Use this after upgrade to 2sxc v14 // Kit.Scrub.All(video.Summary)); // Scrub.All makes sure no HTML makes it into the teaser
            var guidNode = AddTag(itemNode, "guid", video.EntityGuid.ToString());
            AddAttribute(guidNode, "isPermaLink", "false");
            AddTag(itemNode, "pubDate", video.Date.ToString("R"));
            // var enclosureNode = AddTag(itemNode, "enclosure", "");
            // AddAttribute(enclosureNode, "url", "https://youtu.be/" + @video.YouTubeId);
        }

        return File(download: false, fileDownloadName: "rss.xml", contents: rssDoc);
    }

    private XmlElement AddPrefixedTag(XmlElement parent, string prefix, string name, string value = null) {
        var node = parent.OwnerDocument.CreateElement(prefix, name, "");
        node.InnerText = value;
        parent.AppendChild(node);
        return node;
    }

    private XmlElement AddTag(XmlElement parent, string name, string value = null) {
        var node = parent.OwnerDocument.CreateElement(name);
        node.InnerText = value;
        parent.AppendChild(node);
        return node;
    }

    private XmlAttribute AddAttribute(XmlElement parent, string name, string value) {
        var node = parent.OwnerDocument.CreateAttribute(name);
        node.Value = value;
        return parent.Attributes.Append(node);
    }

    private XmlElement AddNamespaceTag(XmlElement parent, string name, string tagNs, string link) {
        var node = parent.OwnerDocument.CreateElement(name, tagNs, link);
        parent.AppendChild(node);
        return node;
    }

    [HttpGet]
    public dynamic Get(int id = -1)
    {
        if (id < 0) {
            var videosQuery = App.Query["VideoList"];
            // videosQuery.Params("pagesize", pagesize.ToString());
            var videosDefault = videosQuery["Default"];
            var videos = AsList(videosDefault);
            var videosPaging = videosQuery["Paging"];
            var paging = AsList(videosPaging);

            return new {
                videos = videos.Select(video =>
                    new {
                        id = video.EntityId,
                        title = video.Title,
                        date = video.Date.ToString("MMM dd, yyyy"),
                        summary = video.Summary,
                        image = video.Image,
                        youTubeId = video.YouTubeId,
                    }
                ),
                paging = paging.Select(p =>
                    new {
                        pageSize = p.PageSize,
                        pageNumber = p.PageNumber,
                        itemCount = p.ItemCount,
                        pageCount = p.PageCount,
                    }
                ),
            };
        } else {
            var videoQuery = App.Query["VideoDetailMobile"];
            videoQuery.Params("id", id.ToString());
            var videoDefault = videoQuery["Default"];
            var video = AsDynamic(videoDefault.First());

            return new {
                id = video.EntityId,
                title = video.Title,
                date = video.Date.ToString("MMM dd, yyyy"),
                summary = video.Summary,
                image = video.Image,
                showNotes = video.ShowNotes,
                youTubeId = video.YouTubeId,
                relatedVideos = (video.RelatedVideos as Dynlist).Select(relatedVideo =>
                    new {
                        id = relatedVideo.EntityId,
                        title = relatedVideo.Title,
                    }
                ),
            };
        }
    }
}
