using System;
using Amazon.S3.Model;
using Orchard.FileSystems.Media;

namespace JG.Orchard.AmazonS3Storage.Models
{
    internal class S3StorageFolder : IStorageFolder
    {
        private readonly S3Object _entry;
        private readonly long _folderSize;

        public S3StorageFolder(S3Object entry, long folderSize)
        {
            // TODO: Complete member initialization
            this._folderSize = folderSize;
            this._entry = entry;
        }

        public string GetPath()
        {
            return _entry.Key;
        }

        public string GetName()
        {
            var tempKey = _entry.Key.Substring(0, _entry.Key.Length - 1); //remove trailing slash
            if (tempKey.Contains("/"))
                tempKey = tempKey.Substring(tempKey.LastIndexOf('/') + 1);
            return tempKey;
        }

        public long GetSize()
        {
            return _folderSize;
        }

        public DateTime GetLastUpdated()
        {
            return _entry.LastModified;
        }

        public IStorageFolder GetParent()
        {
            throw new NotImplementedException();
        }
    }
}
