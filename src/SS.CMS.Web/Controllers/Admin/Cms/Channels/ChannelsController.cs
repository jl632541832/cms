﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SS.CMS.Abstractions;
using SS.CMS.Abstractions.Dto.Request;
using SS.CMS.Abstractions.Dto.Result;
using SS.CMS.Api;
using SS.CMS.Api.Stl;
using SS.CMS.Core;
using SS.CMS.Framework;
using SS.CMS.Plugins;
using SS.CMS.Core.Serialization;
using SS.CMS.Web.Extensions;

namespace SS.CMS.Web.Controllers.Admin.Cms.Channels
{
    [Route("admin/cms/channels/channels")]
    public partial class ChannelsController : ControllerBase
    {
        private const string Route = "";
        private const string RouteGet = "{siteId:int}/{channelId:int}";
        private const string RouteAppend = "actions/append";
        private const string RouteUpload = "actions/upload";
        private const string RouteImport = "actions/import";
        private const string RouteExport = "actions/export";
        private const string RouteOrder = "actions/order";

        private readonly IAuthManager _authManager;
        private readonly ICreateManager _createManager;

        public ChannelsController(IAuthManager authManager, ICreateManager createManager)
        {
            _authManager = authManager;
            _createManager = createManager;
        }

        [HttpGet, Route(Route)]
        public async Task<ActionResult<ChannelsResult>> List([FromQuery] SiteRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasSitePermissionsAsync(request.SiteId,
                    Constants.SitePermissions.Channels))
            {
                return Unauthorized();
            }

            var site = await DataProvider.SiteRepository.GetAsync(request.SiteId);
            if (site == null) return NotFound();

            var channel = await DataProvider.ChannelRepository.GetAsync(request.SiteId);
            var cascade = await DataProvider.ChannelRepository.GetCascadeAsync(site, channel, async summary =>
            {
                var count = await DataProvider.ContentRepository.GetCountAsync(site, summary);
                var groupNames = await DataProvider.ChannelRepository.GetGroupNamesAsync(summary.Id);
                return new
                {
                    Count = count,
                    summary.IndexName,
                    GroupNames = groupNames,
                    summary.Taxis,
                    summary.ParentId
                };
            });

            var indexNames = await DataProvider.ChannelRepository.GetChannelIndexNameListAsync(request.SiteId);
            var groupNameList = await DataProvider.ChannelGroupRepository.GetGroupNamesAsync(request.SiteId);

            var channelTemplates = await DataProvider.TemplateRepository.GetTemplateListByTypeAsync(request.SiteId, TemplateType.ChannelTemplate);
            var contentTemplates = await DataProvider.TemplateRepository.GetTemplateListByTypeAsync(request.SiteId, TemplateType.ContentTemplate);
            var contentPlugins = await PluginContentManager.GetContentModelPluginsAsync();
            var relatedPlugins = await PluginContentManager.GetAllContentRelatedPluginsAsync(false);

            return new ChannelsResult
            {
                Channel = cascade,
                IndexNames = indexNames,
                GroupNames = groupNameList,
                ChannelTemplates = channelTemplates,
                ContentTemplates = contentTemplates,
                ContentPlugins = contentPlugins,
                RelatedPlugins = relatedPlugins
            };
        }

        [HttpPost, Route(RouteAppend)]
        public async Task<ActionResult<List<int>>> Append([FromBody] AppendRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasChannelPermissionsAsync(request.SiteId, request.ParentId, Constants.ChannelPermissions.ChannelAdd))
            {
                return Unauthorized();
            }

            var site = await DataProvider.SiteRepository.GetAsync(request.SiteId);
            if (site == null) return NotFound();

            var parent = await DataProvider.ChannelRepository.GetAsync(request.ParentId);
            if (parent == null) return this.Error("无法确定父栏目");

            var insertedChannelIdHashtable = new Hashtable { [1] = request.ParentId }; //key为栏目的级别，1为第一级栏目

            var channelTemplateId = request.ChannelTemplateId;
            var contentTemplateId = request.ContentTemplateId;
            if (request.IsParentTemplates)
            {
                channelTemplateId = parent.ChannelTemplateId;
                contentTemplateId = parent.ContentTemplateId;
            }

            var channelNames = request.Channels.Split('\n');
            IList<string> nodeIndexNameList = null;
            var expandedChannelIds = new List<int>
            {
                request.SiteId
            };
            foreach (var item in channelNames)
            {
                if (string.IsNullOrEmpty(item)) continue;

                //count为栏目的级别
                var count = StringUtils.GetStartCount('－', item) == 0 ? StringUtils.GetStartCount('-', item) : StringUtils.GetStartCount('－', item);
                var channelName = item.Substring(count, item.Length - count);
                var indexName = string.Empty;
                count++;

                if (!string.IsNullOrEmpty(channelName) && insertedChannelIdHashtable.Contains(count))
                {
                    if (request.IsIndexName)
                    {
                        indexName = channelName.Trim();
                    }

                    if (StringUtils.Contains(channelName, "(") && StringUtils.Contains(channelName, ")"))
                    {
                        var length = channelName.IndexOf(')') - channelName.IndexOf('(');
                        if (length > 0)
                        {
                            indexName = channelName.Substring(channelName.IndexOf('(') + 1, length);
                            channelName = channelName.Substring(0, channelName.IndexOf('('));
                        }
                    }
                    channelName = channelName.Trim();
                    indexName = indexName.Trim(' ', '(', ')');
                    if (!string.IsNullOrEmpty(indexName))
                    {
                        if (nodeIndexNameList == null)
                        {
                            nodeIndexNameList = (await DataProvider.ChannelRepository.GetIndexNameListAsync(request.SiteId)).ToList();
                        }
                        if (nodeIndexNameList.Contains(indexName))
                        {
                            indexName = string.Empty;
                        }
                        else
                        {
                            nodeIndexNameList.Add(indexName);
                        }
                    }

                    var parentId = (int)insertedChannelIdHashtable[count];

                    var insertedChannelId = await DataProvider.ChannelRepository.InsertAsync(request.SiteId, parentId, channelName, indexName, parent.ContentModelPluginId, parent.ContentRelatedPluginIds, channelTemplateId, contentTemplateId);
                    insertedChannelIdHashtable[count + 1] = insertedChannelId;
                    expandedChannelIds.Add(insertedChannelId);

                    await _createManager.CreateChannelAsync(request.SiteId, insertedChannelId);
                }
            }

            return expandedChannelIds;
        }

        [HttpDelete, Route(Route)]
        public async Task<ActionResult<List<int>>> Delete([FromBody] DeleteRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasChannelPermissionsAsync(request.SiteId, request.ChannelId, Constants.ChannelPermissions.ChannelDelete))
            {
                return Unauthorized();
            }

            var site = await DataProvider.SiteRepository.GetAsync(request.SiteId);
            if (site == null) return NotFound();

            var channel = await DataProvider.ChannelRepository.GetAsync(request.ChannelId);
            if (channel == null) return this.Error("无法确定父栏目");

            if (channel.ChannelName != request.ChannelName)
            {
                return this.Error("请检查您输入的栏目名称是否正确");
            }

            var channelIdList = await DataProvider.ChannelRepository.GetChannelIdsAsync(request.SiteId, request.ChannelId, ScopeType.All);

            if (request.DeleteFiles)
            {
                await _createManager.DeleteChannelsAsync(site, channelIdList);
            }

            foreach (var channelId in channelIdList)
            {
                await DataProvider.ContentRepository.RecycleAllAsync(site, channelId, auth.AdminId);
                await DataProvider.ChannelRepository.DeleteAsync(site, channelId, auth.AdminId);
            }

            await auth.AddSiteLogAsync(request.SiteId, "删除栏目", $"栏目:{channel.ChannelName}");

            return new List<int>
            {
                request.SiteId,
                channel.ParentId
            };
        }

        [HttpPost, Route(RouteUpload)]
        public async Task<ActionResult<StringResult>> Upload([FromBody] UploadRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasChannelPermissionsAsync(request.SiteId, request.SiteId, Constants.ChannelPermissions.ChannelAdd))
            {
                return Unauthorized();
            }

            if (request.File == null)
            {
                return this.Error("请选择有效的文件上传");
            }

            var fileName = Path.GetFileName(request.File.FileName);

            var sExt = PathUtils.GetExtension(fileName);
            if (!StringUtils.EqualsIgnoreCase(sExt, ".zip"))
            {
                return this.Error("导入文件为Zip格式，请选择有效的文件上传");
            }

            fileName = $"{StringUtils.GetShortGuid(false)}.zip";
            var filePath = PathUtility.GetTemporaryFilesPath(fileName);
            DirectoryUtils.CreateDirectoryIfNotExists(filePath);
            request.File.CopyTo(new FileStream(filePath, FileMode.Create));

            return new StringResult
            {
                Value = fileName
            };
        }

        [HttpPost, Route(RouteImport)]
        public async Task<ActionResult<List<int>>> Import([FromBody] ImportRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasChannelPermissionsAsync(request.SiteId, request.ChannelId, Constants.ChannelPermissions.ChannelAdd))
            {
                return Unauthorized();
            }

            try
            {
                var site = await DataProvider.SiteRepository.GetAsync(request.SiteId);
                var filePath = PathUtility.GetTemporaryFilesPath(request.FileName);

                var importObject = new ImportObject(site, auth.AdminId);
                await importObject.ImportChannelsAndContentsByZipFileAsync(request.ChannelId, filePath,
                    request.IsOverride, null);

                await auth.AddSiteLogAsync(request.SiteId, "导入栏目");
            }
            catch
            {
                return this.Error("压缩包格式不正确，请上传正确的栏目压缩包");
            }

            return new List<int>
            {
                request.SiteId,
                request.ChannelId
            };
        }

        [HttpPost, Route(RouteExport)]
        public async Task<ActionResult<StringResult>> Export([FromBody] ChannelIdsRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasSitePermissionsAsync(request.SiteId, Constants.SitePermissions.Channels))
            {
                return Unauthorized();
            }

            var site = await DataProvider.SiteRepository.GetAsync(request.SiteId);
            if (site == null) return NotFound();

            var exportObject = new ExportObject(site, auth.AdminId);
            var fileName = await exportObject.ExportChannelsAsync(request.ChannelIds);
            var filePath = PathUtility.GetTemporaryFilesPath(fileName);
            var url = ApiRouteActionsDownload.GetUrl(ApiManager.InnerApiUrl, filePath);

            return new StringResult
            {
                Value = url
            };
        }

        [HttpPost, Route(RouteOrder)]
        public async Task<ActionResult<List<int>>> Order([FromBody] OrderRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin ||
                !await auth.AdminPermissions.HasSitePermissionsAsync(request.SiteId,
                    Constants.SitePermissions.Channels))
            {
                return Unauthorized();
            }

            if (request.IsUp)
            {
                await DataProvider.ChannelRepository.UpdateTaxisUpAsync(request.SiteId, request.ChannelId, request.ParentId, request.Taxis);
            }
            else
            {
                await DataProvider.ChannelRepository.UpdateTaxisDownAsync(request.SiteId, request.ChannelId, request.ParentId, request.Taxis);
            }

            return new List<int>
            {
                request.SiteId,
                request.ChannelId
            };
        }
    }
}
