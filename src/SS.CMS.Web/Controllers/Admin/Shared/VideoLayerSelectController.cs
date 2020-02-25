﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SS.CMS.Abstractions;
using SS.CMS.Abstractions.Dto.Result;
using SS.CMS.Core;
using SS.CMS.Framework;
using SS.CMS.Web.Extensions;

namespace SS.CMS.Web.Controllers.Admin.Shared
{
    [Route("admin/shared/videoLayerSelect")]
    public partial class VideoLayerSelectController : ControllerBase
    {
        private const string Route = "";
        private const string RouteSelect = "actions/select";

        private readonly IAuthManager _authManager;

        public VideoLayerSelectController(IAuthManager authManager)
        {
            _authManager = authManager;
        }

        [HttpGet, Route(Route)]
        public async Task<ActionResult<QueryResult>> List([FromQuery]QueryRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin) return Unauthorized();

            var groups = await DataProvider.LibraryGroupRepository.GetAllAsync(LibraryType.Video);
            groups.Insert(0, new LibraryGroup
            {
                Id = 0,
                GroupName = "全部视频"
            });
            var count = await DataProvider.LibraryVideoRepository.GetCountAsync(request.GroupId, request.Keyword);
            var items = await DataProvider.LibraryVideoRepository.GetAllAsync(request.GroupId, request.Keyword, request.Page, request.PerPage);

            return new QueryResult
            {
                Groups = groups,
                Count = count,
                Items = items
            };
        }

        [HttpPost, Route(RouteSelect)]
        public async Task<ActionResult<StringResult>> Select([FromBody]SelectRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin) return Unauthorized();

            var site = await DataProvider.SiteRepository.GetAsync(request.SiteId);
            var library = await DataProvider.LibraryVideoRepository.GetAsync(request.LibraryId);

            var libraryFilePath = PathUtils.Combine(WebConfigUtils.PhysicalApplicationPath, library.Url);
            if (!FileUtils.IsFileExists(libraryFilePath))
            {
                return this.Error("视频不存在，请重新选择");
            }

            var localDirectoryPath = await PathUtility.GetUploadDirectoryPathAsync(site, UploadType.Video);
            var filePath = PathUtils.Combine(localDirectoryPath, PathUtility.GetUploadFileName(site, libraryFilePath));

            DirectoryUtils.CreateDirectoryIfNotExists(filePath);
            FileUtils.CopyFile(libraryFilePath, filePath);

            var fileUrl = await PageUtility.GetSiteUrlByPhysicalPathAsync(site, filePath, true);

            return new StringResult
            {
                Value = fileUrl
            };
        }
    }
}
