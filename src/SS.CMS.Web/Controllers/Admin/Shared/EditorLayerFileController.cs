﻿using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SS.CMS.Abstractions;
using SS.CMS.Core;
using SS.CMS.Framework;
using SS.CMS.Web.Extensions;

namespace SS.CMS.Web.Controllers.Admin.Shared
{
    [Route("admin/shared/editorLayerFile")]
    public partial class EditorLayerFileController : ControllerBase
    {
        private const string RouteUpload = "actions/upload";

        private readonly IAuthManager _authManager;

        public EditorLayerFileController(IAuthManager authManager)
        {
            _authManager = authManager;
        }

        [HttpPost, Route(RouteUpload)]
        public async Task<ActionResult<UploadResult>> Upload([FromBody]UploadRequest request)
        {
            var auth = await _authManager.GetAdminAsync();
            if (!auth.IsAdminLoggin) return Unauthorized();

            var site = await DataProvider.SiteRepository.GetAsync(request.SiteId);

            if (request.File == null)
            {
                return this.Error("请选择有效的文件上传");
            }

            var fileName = Path.GetFileName(request.File.FileName);

            if (!PathUtility.IsFileExtensionAllowed(site, PathUtils.GetExtension(fileName)))
            {
                return this.Error("文件格式不正确，请更换文件上传!");
            }

            var localDirectoryPath = await PathUtility.GetUploadDirectoryPathAsync(site, UploadType.File);
            var filePath = PathUtils.Combine(localDirectoryPath, PathUtility.GetUploadFileName(site, fileName));

            DirectoryUtils.CreateDirectoryIfNotExists(filePath);
            request.File.CopyTo(new FileStream(filePath, FileMode.Create));

            var fileUrl = await PageUtility.GetSiteUrlByPhysicalPathAsync(site, filePath, true);

            return new UploadResult
            {
                Name = fileName,
                Path = filePath,
                Url = fileUrl
            };
        }
    }
}
