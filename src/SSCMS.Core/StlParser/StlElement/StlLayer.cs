﻿using System.Text;
using System.Threading.Tasks;
using SSCMS.Core.StlParser.Model;
using SSCMS.Parse;
using SSCMS.Services;
using SSCMS.Utils;

namespace SSCMS.Core.StlParser.StlElement
{
    [StlElement(Title = "页面弹层", Description = "通过 stl:layer 标签在模板中显示弹层组件")]
    public class StlLayer
    {
        private StlLayer() { }
        public const string ElementName = "stl:layer";

        [StlAttribute(Title = "触发函数名称")]
        private const string FuncName = nameof(FuncName);

        [StlAttribute(Title = "标题")]
        private const string Title = nameof(Title);

        [StlAttribute(Title = "Url地址")]
        private const string Url = nameof(Url);

        [StlAttribute(Title = "宽度")]
        private const string Width = nameof(Width);

        [StlAttribute(Title = "高度")]
        private const string Height = nameof(Height);

        [StlAttribute(Title = "开启遮罩关闭")]
        private const string ShadeClose = nameof(ShadeClose);

        [StlAttribute(Title = "坐标")]
        private const string Offset = nameof(Offset);

        public static async Task<object> ParseAsync(IParseManager parseManager)
        {
            var funcName = string.Empty;
            var title = string.Empty;
            var url = string.Empty;
            var width = 0;
            var height = 0;
            var shadeClose = true;
            var offset = "auto";

            foreach (var name in parseManager.ContextInfo.Attributes.AllKeys)
            {
                var value = parseManager.ContextInfo.Attributes[name];

                if (StringUtils.EqualsIgnoreCase(name, FuncName))
                {
                    funcName = await parseManager.ReplaceStlEntitiesForAttributeValueAsync(value);
                }
                else if (StringUtils.EqualsIgnoreCase(name, Title))
                {
                    title = await parseManager.ReplaceStlEntitiesForAttributeValueAsync(value);
                }
                else if (StringUtils.EqualsIgnoreCase(name, Url))
                {
                    url = await parseManager.ReplaceStlEntitiesForAttributeValueAsync(value);
                }
                else if (StringUtils.EqualsIgnoreCase(name, Width))
                {
                    width = TranslateUtils.ToInt(value);
                }
                else if (StringUtils.EqualsIgnoreCase(name, Height))
                {
                    height = TranslateUtils.ToInt(value);
                }
                else if (StringUtils.EqualsIgnoreCase(name, ShadeClose))
                {
                    shadeClose = TranslateUtils.ToBool(value);
                }
                else if (StringUtils.EqualsIgnoreCase(name, Offset))
                {
                    offset = value;
                }
            }

            return await ParseImplAsync(parseManager, funcName, title, url, width, height, shadeClose, offset);
        }

        private static async Task<string> ParseImplAsync(IParseManager parseManager, string funcName, string title,
            string url, int width, int height, bool shadeClose, string offset)
        {
            var pageInfo = parseManager.PageInfo;
            var contextInfo = parseManager.ContextInfo;

            await pageInfo.AddPageBodyCodeIfNotExistsAsync(ParsePage.Const.Jquery);
            await pageInfo.AddPageBodyCodeIfNotExistsAsync(ParsePage.Const.Layer);

            var type = 1;
            var content = string.Empty;
            if (!string.IsNullOrEmpty(url))
            {
                type = 2;
                content = $"'{url}'";
            }
            else if (!string.IsNullOrEmpty(contextInfo.InnerHtml))
            {
                var innerBuilder = new StringBuilder(contextInfo.InnerHtml);
                await parseManager.ParseInnerContentAsync(innerBuilder);
                var uniqueId = "Layer_" + pageInfo.UniqueId;
                pageInfo.BodyCodes.Add(uniqueId,
                    $@"<div id=""{uniqueId}"" style=""display: none"">{innerBuilder}</div>");
                content = $"$('#{uniqueId}')";
            }

            var area = string.Empty;
            if (width > 0 || height > 0)
            {
                area = height == 0
                    ? $@"
area: '{width}px',"
                    : $@"
area: ['{width}px', '{height}px'],";
            }

            var offsetStr = StringUtils.StartsWith(offset, "[") ? offset : $"'{offset}'";

            var script =
                $@"layer.open({{type: {type},{area}shadeClose: {shadeClose.ToString().ToLower()},offset:{offsetStr},title: '{title}',content: {content}}});";

            return !string.IsNullOrEmpty(funcName)
                ? $@"<script>function {funcName}(){{{script}}}</script>"
                : $@"<script>$(document).ready(function() {{{script}}});</script>";
        }
    }
}
