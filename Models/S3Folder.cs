using System;

namespace JG.Orchard.AmazonS3Storage.Models {
    public class S3Folder
    {
        public DateTime LastModified { get; set; }
        public string ETag { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public string FullName { get; set; }
        public string ParentPath { get; set; }

        public S3Folder()
        {

        }

        public S3Folder(string key, DateTime lastModified, string eTag)
        {
            LastModified = lastModified;
            ETag = eTag;
            FullName = key;
            Size = 0;

            var parts = key.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            Name = parts[parts.Length - 1];

            if (parts.Length > 1)
            {
                var parentParts = new string[parts.Length - 1];
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    parentParts[i] = parts[i];
                }
                ParentPath = string.Join("/", parentParts);
            }
        }


    }
}