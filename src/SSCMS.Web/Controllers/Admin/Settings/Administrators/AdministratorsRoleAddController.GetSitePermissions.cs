﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SSCMS.Web.Controllers.Admin.Settings.Administrators
{
    public partial class AdministratorsRoleAddController
    {
        [HttpGet, Route(RouteSiteId)]
        public async Task<ActionResult<SitePermissionsResult>> GetSitePermissions([FromRoute] int siteId, [FromQuery] GetRequest request)
        {
            if (!await _authManager.HasAppPermissionsAsync(AuthTypes.AppPermissions.SettingsAdministratorsRole))
            {
                return Unauthorized();
            }

            var allPermissions = _settingsManager.GetPermissions();
            return await GetSitePermissionsObjectAsync(allPermissions, request.RoleId, siteId);
        }
    }
}