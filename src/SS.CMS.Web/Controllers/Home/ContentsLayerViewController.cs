﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SS.CMS.Abstractions;
using SS.CMS.Core;
using SS.CMS.Framework;

namespace SS.CMS.Web.Controllers.Home
{
    [Route("home/contentsLayerView")]
    public partial class ContentsLayerViewController : ControllerBase
    {
        private const string Route = "";

        private readonly IAuthManager _authManager;

        public ContentsLayerViewController(IAuthManager authManager)
        {
            _authManager = authManager;
        }

        [HttpGet, Route(Route)]
        public async Task<ActionResult<GetResult>> Get([FromQuery]GetRequest request)
        {
            var auth = await _authManager.GetUserAsync();
            if (!auth.IsUserLoggin ||
                !await auth.UserPermissions.HasChannelPermissionsAsync(request.SiteId, request.ChannelId, Constants.ChannelPermissions.ContentView))
            {
                return Unauthorized();
            }

            var site = await DataProvider.SiteRepository.GetAsync(request.SiteId);
            if (site == null) return NotFound();

            var channel = await DataProvider.ChannelRepository.GetAsync(request.ChannelId);
            if (channel == null) return NotFound();

            var content = await DataProvider.ContentRepository.GetAsync(site, channel, request.ContentId);
            if (content == null) return NotFound();

            content.Set(ContentAttribute.CheckState, CheckManager.GetCheckState(site, content));

            var channelName = await DataProvider.ChannelRepository.GetChannelNameNavigationAsync(request.SiteId, request.ChannelId);

            var attributes = await ColumnsManager.GetContentListColumnsAsync(site, channel, ColumnsManager.PageType.Contents);

            return new GetResult
            {
                Content = content,
                ChannelName = channelName,
                Attributes = attributes
            };
        }
    }
}
