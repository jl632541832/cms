﻿using System;
using System.Collections;
using SS.CMS.Abstractions.Models;
using SS.CMS.Abstractions.Repositories;

namespace SS.CMS.Abstractions.Services
{
    public partial interface IFileManager
    {
        void ChangeSiteDir(string parentPsPath, string oldPsDir, string newPsDir);

        void DeleteSiteFiles(SiteInfo siteInfo);

        void ImportSiteFiles(SiteInfo siteInfo, string siteTemplatePath, bool isOverride);

        void ChangeParentSite(ISiteRepository siteRepository, int oldParentSiteId, int newParentSiteId, int siteId, string siteDir);

        void ChangeToHeadquarters(SiteInfo siteInfo, bool isMoveFiles);

        void ChangeToSubSite(SiteInfo siteInfo, string psDir, ArrayList fileSystemNameArrayList);
    }
}
