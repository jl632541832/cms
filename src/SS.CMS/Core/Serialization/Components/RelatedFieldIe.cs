﻿using System.Collections.Generic;
using System.Threading.Tasks;
using SS.CMS.Abstractions;
using SS.CMS.Core.Serialization.Atom.Atom.Core;
using SS.CMS.Framework;

namespace SS.CMS.Core.Serialization.Components
{
	internal class RelatedFieldIe
	{
		private readonly Site _site;
		private readonly string _directoryPath;

        public RelatedFieldIe(Site site, string directoryPath)
		{
            _site = site;
			_directoryPath = directoryPath;
		}

        public async Task ExportRelatedFieldAsync(RelatedField relatedField)
		{
            var filePath = _directoryPath + PathUtils.SeparatorChar + relatedField.Id + ".xml";

            var feed = ExportRelatedFieldInfo(relatedField);

            var relatedFieldItemInfoList = await DataProvider.RelatedFieldItemRepository.GetListAsync(_site.Id, relatedField.Id, 0);

            foreach (var relatedFieldItemInfo in relatedFieldItemInfoList)
			{
                await AddAtomEntryAsync(feed, _site.Id, relatedFieldItemInfo, 1);
			}
			feed.Save(filePath);
		}

        private static AtomFeed ExportRelatedFieldInfo(RelatedField relatedField)
		{
			var feed = AtomUtility.GetEmptyFeed();

            AtomUtility.AddDcElement(feed.AdditionalElements, new List<string> { nameof(RelatedField.Id), "RelatedFieldID" }, relatedField.Id.ToString());
            AtomUtility.AddDcElement(feed.AdditionalElements, new List<string> { nameof(RelatedField.Title), "RelatedFieldName" }, relatedField.Title);
            AtomUtility.AddDcElement(feed.AdditionalElements, new List<string> { nameof(RelatedField.SiteId), "PublishmentSystemID" }, relatedField.SiteId.ToString());

            return feed;
		}

        private static async Task AddAtomEntryAsync(AtomFeed feed, int siteId, RelatedFieldItem relatedFieldItem, int level)
		{
			var entry = AtomUtility.GetEmptyEntry();

            AtomUtility.AddDcElement(entry.AdditionalElements, new List<string> { nameof(RelatedFieldItem.Id), "ID" }, relatedFieldItem.Id.ToString());
            AtomUtility.AddDcElement(entry.AdditionalElements, new List<string> { nameof(RelatedFieldItem.RelatedFieldId), "RelatedFieldID" }, relatedFieldItem.RelatedFieldId.ToString());
            AtomUtility.AddDcElement(entry.AdditionalElements, nameof(RelatedFieldItem.Label), relatedFieldItem.Label);
            AtomUtility.AddDcElement(entry.AdditionalElements, nameof(RelatedFieldItem.Value), relatedFieldItem.Value);
            AtomUtility.AddDcElement(entry.AdditionalElements, new List<string> { nameof(RelatedFieldItem.ParentId), "ParentID" }, relatedFieldItem.ParentId.ToString());
            AtomUtility.AddDcElement(entry.AdditionalElements, nameof(RelatedFieldItem.Taxis), relatedFieldItem.Taxis.ToString());
            AtomUtility.AddDcElement(entry.AdditionalElements, "Level", level.ToString());

            feed.Entries.Add(entry);

            var relatedFieldItemInfoList = await DataProvider.RelatedFieldItemRepository.GetListAsync(siteId, relatedFieldItem.RelatedFieldId, relatedFieldItem.Id);

            foreach (var itemInfo in relatedFieldItemInfoList)
            {
                await AddAtomEntryAsync(feed, siteId, itemInfo, level + 1);
            }
		}

		public async Task ImportRelatedFieldAsync(bool overwrite)
		{
			if (!DirectoryUtils.IsDirectoryExists(_directoryPath)) return;
			var filePaths = DirectoryUtils.GetFilePaths(_directoryPath);

			foreach (var filePath in filePaths)
			{
                var feed = AtomFeed.Load(FileUtils.GetFileStreamReadOnly(filePath));

                var title = AtomUtility.GetDcElementContent(feed.AdditionalElements, new List<string> { nameof(RelatedField.Title), "RelatedFieldName" });

                var relatedFieldInfo = new RelatedField
                {
                    Id = 0,
                    Title = title,
                    SiteId = _site.Id
                };

                var srcRelatedFieldInfo = await DataProvider.RelatedFieldRepository.GetRelatedFieldAsync(_site.Id, title);
                if (srcRelatedFieldInfo != null)
                {
                    if (overwrite)
                    {
                        await DataProvider.RelatedFieldRepository.DeleteAsync(srcRelatedFieldInfo.Id);
                    }
                    else
                    {
                        relatedFieldInfo.Title = await DataProvider.RelatedFieldRepository.GetImportTitleAsync(_site.Id, relatedFieldInfo.Title);
                    }
                }

                var relatedFieldId = await DataProvider.RelatedFieldRepository.InsertAsync(relatedFieldInfo);

                var lastInertedLevel = 1;
                var lastInsertedParentId = 0;
                var lastInsertedId = 0;
				foreach (AtomEntry entry in feed.Entries)
				{
                    var itemName = AtomUtility.GetDcElementContent(entry.AdditionalElements, nameof(RelatedFieldItem.Label));
                    var itemValue = AtomUtility.GetDcElementContent(entry.AdditionalElements, nameof(RelatedFieldItem.Value));
                    var level = TranslateUtils.ToInt(AtomUtility.GetDcElementContent(entry.AdditionalElements, "Level"));
                    var parentId = 0;
                    if (level > 1)
                    {
                        parentId = level != lastInertedLevel ? lastInsertedId : lastInsertedParentId;
                    }

                    var relatedFieldItemInfo = new RelatedFieldItem
                    {
                        Id = 0,
                        RelatedFieldId = relatedFieldId,
                        Label = itemName,
                        Value = itemValue,
                        ParentId = parentId,
                        Taxis = 0
                    };
                    lastInsertedId = await DataProvider.RelatedFieldItemRepository.InsertAsync(relatedFieldItemInfo);
                    lastInsertedParentId = parentId;
                    lastInertedLevel = level;
				}
			}
		}

	}
}
