﻿using System;
using System.Collections.Generic;
using SS.CMS.Abstractions.Enums;
using SS.CMS.Core.Cache;
using SS.CMS.Core.Cache.Stl;
using SS.CMS.Core.StlParser.Models;
using SS.CMS.Core.StlParser.Utility;
using SS.CMS.Utils;
using SS.CMS.Utils.Enumerations;

namespace SS.CMS.Core.StlParser.StlElement
{
    [StlElement(Title = "显示数值", Description = "通过 stl:count 标签在模板中显示统计数字")]
    public class StlCount
    {
        private StlCount() { }
        public const string ElementName = "stl:count";

        [StlAttribute(Title = "需要获取值的类型")]
        private const string Type = nameof(Type);

        [StlAttribute(Title = "栏目索引")]
        private const string ChannelIndex = nameof(ChannelIndex);

        [StlAttribute(Title = "栏目名称")]
        private const string ChannelName = nameof(ChannelName);

        [StlAttribute(Title = "上级栏目的级别")]
        private const string UpLevel = nameof(UpLevel);

        [StlAttribute(Title = "从首页向下的栏目级别")]
        private const string TopLevel = nameof(TopLevel);

        [StlAttribute(Title = "内容范围")]
        private const string Scope = nameof(Scope);

        [StlAttribute(Title = "时间段")]
        private const string Since = nameof(Since);


        public const string TypeChannels = "Channels";
        public const string TypeContents = "Contents";

        public static SortedList<string, string> TypeList => new SortedList<string, string>
        {
            {TypeChannels, "栏目数"},
            {TypeContents, "内容数"}
        };

        public static string Parse(ParseContext parseContext)
        {
            var type = string.Empty;
            var channelIndex = string.Empty;
            var channelName = string.Empty;
            var upLevel = 0;
            var topLevel = -1;
            var scope = ScopeType.Self;
            var since = string.Empty;

            foreach (var name in parseContext.Attributes.AllKeys)
            {
                var value = parseContext.Attributes[name];

                if (StringUtils.EqualsIgnoreCase(name, Type))
                {
                    type = value;
                }
                else if (StringUtils.EqualsIgnoreCase(name, ChannelIndex))
                {
                    channelIndex = value;
                }
                else if (StringUtils.EqualsIgnoreCase(name, ChannelName))
                {
                    channelName = value;
                }
                else if (StringUtils.EqualsIgnoreCase(name, UpLevel))
                {
                    upLevel = TranslateUtils.ToInt(value);
                }
                else if (StringUtils.EqualsIgnoreCase(name, TopLevel))
                {
                    topLevel = TranslateUtils.ToInt(value);
                }
                else if (StringUtils.EqualsIgnoreCase(name, Scope))
                {
                    scope = ScopeType.Parse(value);
                }
                else if (StringUtils.EqualsIgnoreCase(name, Since))
                {
                    since = value;
                }
            }

            return ParseImpl(parseContext, type, channelIndex, channelName, upLevel, topLevel, scope, since);
        }

        private static string ParseImpl(ParseContext parseContext, string type, string channelIndex, string channelName, int upLevel, int topLevel, ScopeType scope, string since)
        {
            var count = 0;

            var sinceDate = DateUtils.SqlMinValue;
            if (!string.IsNullOrEmpty(since))
            {
                sinceDate = DateTime.Now.AddHours(-DateUtils.GetSinceHours(since));
            }

            if (string.IsNullOrEmpty(type) || StringUtils.EqualsIgnoreCase(type, TypeContents))
            {
                var channelId = StlDataUtility.GetChannelIdByLevel(parseContext.SiteId, parseContext.ChannelId, upLevel, topLevel);
                channelId = StlDataUtility.GetChannelIdByChannelIdOrChannelIndexOrChannelName(parseContext.SiteId, channelId, channelIndex, channelName);

                var nodeInfo = ChannelManager.GetChannelInfo(parseContext.SiteId, channelId);
                var channelIdList = ChannelManager.GetChannelIdList(nodeInfo, scope, string.Empty, string.Empty, string.Empty);
                foreach (var theChannelId in channelIdList)
                {
                    var channelInfo = ChannelManager.GetChannelInfo(parseContext.SiteId, theChannelId);
                    count += channelInfo.ContentRepository.StlGetCountOfContentAdd(parseContext.SiteId, channelInfo, ScopeType.Self, sinceDate, DateTime.Now.AddDays(1), string.Empty, true);
                }
            }
            else if (StringUtils.EqualsIgnoreCase(type, TypeChannels))
            {
                var channelId = StlDataUtility.GetChannelIdByLevel(parseContext.SiteId, parseContext.ChannelId, upLevel, topLevel);
                channelId = StlDataUtility.GetChannelIdByChannelIdOrChannelIndexOrChannelName(parseContext.SiteId, channelId, channelIndex, channelName);

                var nodeInfo = ChannelManager.GetChannelInfo(parseContext.SiteId, channelId);
                count = nodeInfo.ChildrenCount;
            }

            return count.ToString();
        }
    }
}
