using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.S3;
using Amazon.S3.IO;
using Amazon.S3.Model;
using Amazon.S3.Util;
using JG.Orchard.AmazonS3Storage.Models;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Environment.Extensions;
using Orchard.FileSystems.Media;
using Orchard.Localization;
using Orchard.Logging;

namespace JG.Orchard.AmazonS3Storage.Providers
{
    //[OrchardFeature("AmazonS3Media.Storage")]
    [OrchardSuppressDependency("Orchard.FileSystems.Media.FileSystemStorageProvider")]
    public class S3StorageProvider : IStorageProvider, IDisposable
    {

        private AmazonS3Config _s3Config;
        private readonly IOrchardServices _services;
        private IAmazonS3 _client = null;
        public ILogger Logger { get; set; }

        public S3StorageProvider(IOrchardServices services)
        {
            _services = services;


            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;

        }

        public void EnsureInitialized() {
            if (_s3Config == null) {
                var regionEndpoint=this.RegionEndpoint;
                if (regionEndpoint==null)
                    return;
                try {
                    _s3Config = new AmazonS3Config() {
                        RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(RegionEndpoint),
                    };
                }
                catch (Exception ex) {
                    Logger.Error(ex, "Failed to create S3Config");
                    return;
                }
            }

            if (_client == null) {
                try {
                    _client = Amazon.AWSClientFactory.CreateAmazonS3Client(AWSAccessKey, AWSSecretKey, _s3Config);
                    Logger.Debug("Initialized a new AmazonS3Client");
                }
                catch (Exception ex) {
                    Logger.Error(ex, "failed to create S3Client");
                }
            } 
        }

        public string PublicPath
        {
            get
            {
                EnsureInitialized();
                if (_s3Config == null)
                    return "";

                if (_s3Config.RegionEndpoint != null) {
                    var endpoint = _s3Config.RegionEndpoint.GetEndpointForService("s3");
                    return string.Format("{0}{1}/{2}", 
                        endpoint.HTTPS ? "https://" : "http://",
                        endpoint.Hostname, 
                        BucketName);
                }

                return "http://s3.amazonaws.com/" + "/" + BucketName;
            }
        }
        public string AWSAccessKey
        {
            get {
                var settings = _services.WorkContext.CurrentSite.As<S3StorageProviderSettingsPart>();
                if (settings != null)
                    return settings.AWSAccessKey;
                return null;
            }
        }
        public string AWSSecretKey
        {
            get
            {
                var settings = _services.WorkContext.CurrentSite.As<S3StorageProviderSettingsPart>();
                if (settings != null)
                    return settings.AWSSecretKey;
                return null;
            }
        }
        public string BucketName
        {
            get
            {
                var settings = _services.WorkContext.CurrentSite.As<S3StorageProviderSettingsPart>();
                if (settings != null)
                    return settings.BucketName;
                return null;
            }
        }
        public string RegionEndpoint
        {
            get {
                var settings = _services.WorkContext.CurrentSite.As<S3StorageProviderSettingsPart>();
                if (settings != null)
                    return settings.RegionEndpoint;
                return null;
            }
        }

        public Localizer T { get; set; }


        private static string ConvertToRelativeUriPath(string path)
        {
            var newPath = path.Replace(@"\", "/");

            if (newPath.StartsWith("/") || newPath.StartsWith("http://") || newPath.StartsWith("https://"))
                throw new ArgumentException("Path must be relative");

            return newPath;
        }


        public bool FileExists(string path) {
            EnsureInitialized();
            if (_s3Config == null) return false;

            var files = new List<S3StorageFile>();
                var request = new ListObjectsRequest();
                request.BucketName = BucketName;
                request.Prefix = path;

                var response = _client.ListObjects(request);
                foreach (var entry in response.S3Objects.Where(e => e.Key.Last() != '/'))
                {
                    //var mimeType = AmazonS3Util.MimeTypeFromExtension(entry.Key.Substring(entry.Key.LastIndexOf(".", System.StringComparison.Ordinal)));
                    files.Add(new S3StorageFile(new S3FileInfo(_client, BucketName, entry.Key), this));
                }

            return files.Any();
        }

        /// <summary>
        /// Retrieves the public URL for a given file within the storage provider.
        /// </summary>
        /// <param name="path">The relative path within the storage provider.</param>
        /// <returns>The public URL.</returns>
        public string GetPublicUrl(string path)
        {
            return string.IsNullOrEmpty(path) ? PublicPath : Path.Combine(PublicPath, path).Replace(Path.DirectorySeparatorChar, '/');
        }

        public string GetStoragePath(string url) {
            return url.Replace(PublicPath, "").TrimStart('/');
        }

        /// <summary>
        /// Retrieves a file within the storage provider.
        /// </summary>
        /// <param name="path">The relative path to the file within the storage provider.</param>
        /// <returns>The file.</returns>
        /// <exception cref="ArgumentException">If the file is not found.</exception>
        public IStorageFile GetFile(string path) {
            EnsureInitialized();
            if (_s3Config==null) return null;
            // seperate folder form file
                var request = new GetObjectRequest();
                request.BucketName = BucketName;
                request.Key = path;
                request.ResponseExpires = DateTime.Now.AddMinutes(5);

                using (GetObjectResponse response = _client.GetObject(request)) {
                    var fileInfo = new S3FileInfo(_client, BucketName, response.Key);
                    return new S3StorageFile(fileInfo, this);

                }

                //using (GetObjectResponse response = client.GetObject(request))
                //{
                //    response.Key.Substring()
                //   foreach (var entry in response.S3Objects.Where(e => e.Key == path))
                //   {
                //       var mimeType = AmazonS3Util.MimeTypeFromExtension(entry.Key.Substring(entry.Key.LastIndexOf(".", System.StringComparison.Ordinal)));
                //       return new S3StorageFile(entry, mimeType);
                //   }
                //}



        }

        /// <summary>
        /// Lists the files within a storage provider's path.
        /// </summary>
        /// <param name="path">The relative path to the folder which files to list.</param>
        /// <returns>The list of files in the folder.</returns>
        public IEnumerable<IStorageFile> ListFiles(string path)
        {
            EnsureInitialized();
            if (_s3Config==null) return null;
            var files = new List<S3StorageFile>();
                var request = new ListObjectsRequest();
                request.BucketName = BucketName;
                request.Prefix = path;

                var response = _client.ListObjects(request);
                foreach (var entry in response.S3Objects.Where(e => e.Key.Last() != '/'))
                {
                    var mimeType = AmazonS3Util.MimeTypeFromExtension(entry.Key.Substring(entry.Key.LastIndexOf(".", System.StringComparison.Ordinal)));
                    files.Add(new S3StorageFile(new S3FileInfo(_client, BucketName, entry.Key), this));
                }

            return files;
        }

        public bool FolderExists(string path)
        {
            return false;
            /*
            var folders = new List<S3StorageFolder>();

            using (var client = Amazon.AWSClientFactory.CreateAmazonS3Client(AWSAccessKey, AWSSecretKey, _s3Config))
            {
                path = path ?? "";
                var request = new ListObjectsRequest();
                request.BucketName = BucketName;
                request.Prefix = string.Format(@"{0}/", path);
                request.Delimiter=@"/";

                var response = client.ListObjects(request);
                foreach (var subFolder in response.CommonPrefixes)
                {
                    var folderSize = ListFiles(subFolder).Sum(x => x.GetSize());
                    S3Object entry=new S3Object();
                    entry.

                    new S3StorageFolder(entry, folderSize);
                    folders.Add(new S3StorageFolder(subFolder, folderSize));
                }
            }

            return folders.Any();*/
        }

        /// <summary>
        /// Lists the folders within a storage provider's path.
        /// </summary>
        /// <param name="path">The relative path to the folder which folders to list.</param>
        /// <returns>The list of folders in the folder.</returns>
        public IEnumerable<IStorageFolder> ListFolders(string path)
        {
            EnsureInitialized();
            if (_s3Config==null) return null;
            var folders = new List<S3StorageFolder>();

                path = path ?? "";
                ListObjectsRequest request = new ListObjectsRequest();
                request.BucketName = BucketName;

                var depth = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Length;
                ListObjectsResponse response = _client.ListObjects(request);
                var folderObjects = response.S3Objects
                                    .Where(o => o.Key.EndsWith("/")
                                        && o.Key.StartsWith(path)
                                        && o.Key.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Length == depth + 1)
                                        .ToList();

                foreach (var fo in folderObjects)
                {
                    var folderSize = ListFiles(fo.Key).Sum(x => x.GetSize());
                    folders.Add(new S3StorageFolder(fo, folderSize));
                }

            return folders;
        }

        /// <summary>
        /// Tries to create a folder in the storage provider.
        /// </summary>
        /// <param name="path">The relative path to the folder to be created.</param>
        /// <returns>True if success; False otherwise.</returns>
        public bool TryCreateFolder(string path)
        {
            EnsureInitialized();
            if (_s3Config==null) return false;
            try
            {
                    var key = string.Format(@"{0}/", path);
                    var request = new PutObjectRequest();
                    request.BucketName = BucketName;
                    request.Key = key;
                    request.InputStream = new MemoryStream();
                    _client.PutObject(request);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates a folder in the storage provider.
        /// </summary>
        /// <param name="path">The relative path to the folder to be created.</param>
        /// <exception cref="ArgumentException">If the folder already exists.</exception>
        public void CreateFolder(string path)
        {
            EnsureInitialized();
            if (_s3Config==null) return;
            try
            {
                    var key = string.Format(@"{0}/", path);
                    var request = new PutObjectRequest();
                    request.BucketName = BucketName;
                    request.Key = key;
                    request.InputStream = new MemoryStream();
                    _client.PutObject(request);

            }
            catch (Exception ex)
            {
                throw new ArgumentException(T("Directory {0} already exists", path).ToString(), ex);
            }
        }

        public void DeleteFolder(string path)
        {
            using (var client = Amazon.AWSClientFactory.CreateAmazonS3Client(AWSAccessKey, AWSSecretKey, _s3Config))
            {
                DeleteFolder(path, client);
            }
        }

        private void DeleteFolder(string path, IAmazonS3 client)
        {
            EnsureInitialized();
            if (_s3Config==null) return;
            //TODO: Refractor to use async deletion?
            foreach (var folder in ListFolders(path))
            {
                DeleteFolder(folder.GetPath(), client);
            }

            foreach (var file in ListFiles(path))
            {
                DeleteFile(file.GetPath(), client);
            }

            var request = new DeleteObjectRequest()
            {
                BucketName = BucketName,
                Key = path
            };

            DeleteObjectResponse response = client.DeleteObject(request);
        }

        public void RenameFolder(string oldPath, string newPath)
        {
            // Todo: recursive on all keys with prefix
            throw new NotImplementedException("Folder renaming currently not supported");
        }

        public void DeleteFile(string path)
        {
            EnsureInitialized();
            if (_s3Config==null) return;
            DeleteFile(path, _client);
        }

        private void DeleteFile(string path, IAmazonS3 client)
        {
            var request = new DeleteObjectRequest()
            {
                BucketName = BucketName,
                Key = path
            };

            DeleteObjectResponse response = client.DeleteObject(request);
        }

        public void RenameFile(string oldPath, string newPath)
        {
            using (var client = Amazon.AWSClientFactory.CreateAmazonS3Client(AWSAccessKey, AWSSecretKey, _s3Config))
            {
                RenameObject(oldPath, newPath, client);

                //Delete the original
                DeleteFile(oldPath);
            }
        }

        public IStorageFile CreateFile(string path)
        {
            //throw new NotImplementedException("File creation currently not supported.");
            Logger.Information("CreateFile");
            PutObjectRequest request = new PutObjectRequest
            {

                BucketName = BucketName,
                Key = path,
                CannedACL = S3CannedACL.PublicRead,
                InputStream = new MemoryStream(),
                Timeout = TimeSpan.FromSeconds(300),
                ReadWriteTimeout = TimeSpan.FromMinutes(5)
            };

            //request.WithBucketName().WithKey(path).WithCannedACL(S3CannedACL.PublicRead).WithInputStream(inputStream);

            // add far distance experiy date
            request.Headers["Expires"] = DateTime.Now.AddYears(10).ToString("ddd, dd, MMM yyyy hh:mm:ss") + " GMT";
            request.Headers["x-amz-acl"] = "public-read";

            var response = _client.PutObject(request);

            var fileInfo = new S3FileInfo(_client, BucketName, path);
            return new S3StorageFile(fileInfo, this);
        }

        private void RenameObject(string oldPath, string newPath, IAmazonS3 client)
        {
            CopyObjectRequest copyRequest = new CopyObjectRequest();
            copyRequest.SourceBucket = BucketName;
            copyRequest.SourceKey = oldPath;
            copyRequest.DestinationBucket = BucketName;
            copyRequest.DestinationKey = newPath;
            copyRequest.CannedACL = S3CannedACL.PublicRead;

            client.CopyObject(copyRequest);
        }

        /// <summary>
        /// Tries to save a stream in the storage provider.
        /// </summary>
        /// <param name="path">The relative path to the file to be created.</param>
        /// <param name="inputStream">The stream to be saved.</param>
        /// <returns>True if success; False otherwise.</returns>
        public bool TrySaveStream(string path, Stream inputStream)
        {
            try
            {
                SaveStream(path, inputStream);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public void SaveStream(string path, Stream inputStream)
        {
            Logger.Information("Save Stream " + path);
            PutObjectRequest request = new PutObjectRequest
            {

                BucketName = BucketName,
                Key = path,
                CannedACL = S3CannedACL.PublicRead,
                InputStream = inputStream,
                Timeout = TimeSpan.FromSeconds(300),
                ReadWriteTimeout = TimeSpan.FromMinutes(5)
            };

            //request.WithBucketName().WithKey(path).WithCannedACL(S3CannedACL.PublicRead).WithInputStream(inputStream);

            // add far distance experiy date
            request.Headers["Expires"] = DateTime.Now.AddYears(10).ToString("ddd, dd, MMM yyyy hh:mm:ss") + " GMT";
            request.Headers["x-amz-acl"] = "public-read";

            var response = _client.PutObject(request);
        }

        public string Combine(string path1, string path2)
        {
            if (path1.EndsWith("/") || path2.StartsWith("/"))
                return string.Format("{0}{1}", path1, path2);

            return string.Format("{0}/{1}", path1, path2);


        }

        public void SetFileAcl(string key) {
            var request = new PutACLRequest();
            request.Key = key;
            request.BucketName = BucketName;
            request.CannedACL = S3CannedACL.PublicRead;
            var response = _client.PutACL(request);
        }

        public void Dispose() {
            if (_client != null) {
                _client.Dispose();
                Logger.Debug("Disposed of an AmazonS3Client");
            }
        }
    }
}
