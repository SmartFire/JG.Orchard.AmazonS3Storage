using System;
using System.IO;

namespace JG.Orchard.AmazonS3Storage.Models
{
    public class S3File
    {
        public S3File()
        {

        }

        public S3File(string key, long size, DateTime lastModified, string eTag)
        {
            Name = Path.GetFileName(key);
            FullName = key;
            Size = size;
            LastModified = lastModified;
            ETag = eTag;
        }
        public string Name { get; set; }
        public string FullName { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public string ETag { get; set; }
    }
}
