﻿using System.Collections.Generic;
using SS.CMS.Abstractions.Dto.Request;

namespace SS.CMS.Web.Controllers.Admin.Cms.Channels
{
    public partial class ChannelsLayerGroupController
    {
        public class SubmitRequest : ChannelRequest
        {
            public List<int> ChannelIds { get; set; }
            public bool IsCancel { get; set; }
            public IEnumerable<string> GroupNames { get; set; }
        }

        public class AddRequest : ChannelRequest
        {
            public List<int> ChannelIds { get; set; }
            public string GroupName { get; set; }
            public string Description { get; set; }
        }
    }
}