#if NETCOREAPP // Oqtane
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
#else // DNN
using System.Web.Http;
using DotNetNuke.Web.Api;
#endif

using System.Linq;
using Dynlist = System.Collections.Generic.IEnumerable<dynamic>;

[AllowAnonymous]
public class VideoController : Custom.Hybrid.Api12
{
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
