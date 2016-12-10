﻿using System;
using System.Collections.Generic;

namespace Orchard.ContentManagement
{
    public class DefaultContentManagerSession : IContentManagerSession
    {
        private readonly IDictionary<int, ContentItem> _itemByVersionId = new Dictionary<int, ContentItem>();
        private readonly IDictionary<Tuple<int, int>, ContentItem> _itemByContentItemId = new Dictionary<Tuple<int, int>, ContentItem>();
        private readonly IDictionary<int, ContentItem> _publishedItemsById = new Dictionary<int, ContentItem>();
        private readonly IDictionary<int, ContentItem> _latestItemsById = new Dictionary<int, ContentItem>();

        private bool _hasItems;

        public void Store(ContentItem item)
        {
            _hasItems = true;

            _itemByVersionId.Add(item.Id, item);
            _itemByContentItemId.Add(Tuple.Create(item.ContentItemId, item.Number), item);

            // is it the latest version ?
            if (item.Latest)
            {
                _latestItemsById[item.ContentItemId] = item;
            }

            // is it the Published version ?
            if (item.Published)
            {
                _publishedItemsById[item.ContentItemId] = item;
            }
        }

        public bool RecallVersionId(int id, out ContentItem item)
        {
            if (!_hasItems)
            {
                item = null;
                return false;
            }

            return _itemByVersionId.TryGetValue(id, out item);
        }

        public bool RecallContentItemId(int contentItemId, int versionNumber, out ContentItem item)
        {
            if (!_hasItems)
            {
                item = null;
                return false;
            }

            return _itemByContentItemId.TryGetValue(Tuple.Create(contentItemId, versionNumber), out item);
        }

        public bool RecallPublishedItemId(int id, out ContentItem item)
        {
            if (!_hasItems)
            {
                item = null;
                return false;
            }

            if (_publishedItemsById.TryGetValue(id, out item))
            {
                if (!item.Published)
                {
                    _publishedItemsById.Remove(id);
                    item = null;
                }
            }

            return item != null;
        }

        public bool RecallLatestItemId(int id, out ContentItem item)
        {
            if (!_hasItems)
            {
                item = null;
                return false;
            }

            if (_latestItemsById.TryGetValue(id, out item))
            {
                if (!item.Latest)
                {
                    _latestItemsById.Remove(id);
                    item = null;
                }
            }

            return item != null;
        }

        public void Clear()
        {
            _itemByVersionId.Clear();
            _itemByContentItemId.Clear();
            _publishedItemsById.Clear();
            _latestItemsById.Clear();
            _hasItems = false;
        }
    }
}