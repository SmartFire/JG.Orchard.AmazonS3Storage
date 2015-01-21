using System;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Amazon;
using Amazon.EC2.Util;
using Amazon.Runtime.Internal.Util;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using JG.Orchard.AmazonS3Storage.Models;
using JG.Orchard.AmazonS3Storage.Providers;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.FileSystems.Media;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.UI.Notify;
using ILogger = Orchard.Logging.ILogger;

namespace JG.Orchard.AmazonS3Storage.Drivers
{
    public class S3StorageProviderSettingsViewModel {
        public string AWSAccessKey { get; set; }
        public string AWSSecretKey { get; set; }
        public string BucketName { get; set; }
        public string RegionEndpoint { get; set; }
        public string IAMRole { get; set; }
    }
    public class S3StorageProviderSettingsPartDriver : ContentPartDriver<S3StorageProviderSettingsPart> {
        private readonly IStorageProvider _storageProvider;
        public ILogger Logger { get; set; }
        private readonly INotifier _notifier;

        public S3StorageProviderSettingsPartDriver(IStorageProvider storageProvider, INotifier notifier)
        {
            _storageProvider = storageProvider;
            _notifier = notifier;
            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }

        public Localizer T { get; set; }

        protected override string Prefix { get { return "S3StorageProviderSettings"; } }

        protected override DriverResult Editor(S3StorageProviderSettingsPart part, dynamic shapeHelper) {
            return Editor(part, null, shapeHelper);
        }


        protected override DriverResult Editor(S3StorageProviderSettingsPart part, IUpdateModel updater, dynamic shapeHelper)
        {
            return ContentShape("Parts_AmazonS3Media_SiteSettings", () =>
            {
                    if (updater != null && updater.TryUpdateModel(part, Prefix, null, null)) {
                        ValidateS3Connection(part, updater);
                    }
                    return shapeHelper.EditorTemplate(TemplateName: "Parts.AmazonS3Media.SiteSettings",
                        Model: part.Record, 
                        Prefix: Prefix); 
                })
                .OnGroup("Amazon S3");
        }

        private void ValidateS3Connection(S3StorageProviderSettingsPart part, IUpdateModel updater) {
            var provider = _storageProvider as S3StorageProvider;

            if (provider == null) return;

            IAmazonS3 client = null;
            try
            {
                if (part.UseCustomCredentials) {
                    bool valid = true;
                    if (string.IsNullOrWhiteSpace(part.AWSAccessKey)) {
                        updater.AddModelError("AWSAccessKey", T("Specify a value for AWS Access Key"));
                        valid = false;
                    }
                    if (string.IsNullOrWhiteSpace(part.AWSSecretKey))
                    {
                        updater.AddModelError("AWSAccessKey", T("Specify a value for AWS Secret Key"));
                        valid = false;
                    }
                    if (string.IsNullOrWhiteSpace(part.RegionEndpoint))
                    {
                        updater.AddModelError("AWSAccessKey", T("Specify a value for S3 Region Endpoint"));
                        valid = false;
                    }

                    if (!valid)
                        return;

                    client = provider.CreateClientFromCustomCredentials(part.AWSAccessKey, part.AWSSecretKey, Amazon.RegionEndpoint.GetBySystemName(part.RegionEndpoint));
                    if (client != null)
                        _notifier.Information(T("Connecting using custom credentials: OK"));
                }
                else {
                    var iamCredentials = provider.GetIAMCredentials();
                    client = provider.CreateClientFromIAMCredentials(iamCredentials);
                    if (client != null)
                        _notifier.Information(T("Connecting using IAM role: OK"));
                }

                // Check AWS credentials, bucket name and bucket permissions
                string bucketName = part.Record.BucketName;
                GetPreSignedUrlRequest request = new GetPreSignedUrlRequest();
                request.BucketName = bucketName;
                if (AWSConfigs.S3Config.UseSignatureVersion4)
                    request.Expires = DateTime.Now.AddDays(6);
                else
                    request.Expires = new DateTime(2019, 12, 31);
                request.Verb = HttpVerb.HEAD;
                request.Protocol = Protocol.HTTP;
                string url = client.GetPreSignedURL(request);
                Uri uri = new Uri(url);


                if (!AmazonS3Util.DoesS3BucketExist(client, bucketName)) {
                    updater.AddModelError("Settings", T("Invalid bucket name. No bucket by the name {0} exists.", part.Record.BucketName));
                }
                else {
                    // Check for read/write permissions
                    var acl = client.GetACL(new GetACLRequest() {
                        BucketName = bucketName
                    });

                    var grants = acl.AccessControlList.Grants;

                    if (!grants.Any(x => x.Permission == S3Permission.FULL_CONTROL)) {
                        if (!grants.Any(x => x.Permission == S3Permission.WRITE)) {
                            updater.AddModelError("Settings", T("You don't have write access to this bucket"));
                        }
                        if (!grants.Any(x => x.Permission == S3Permission.READ)) {
                            updater.AddModelError("Settings", T("You don't have read access to this bucket"));
                        }
                    }
                }

                _notifier.Information(T("All settings look okay"));
            }
            catch (AmazonS3Exception ex) {
                if (ex.ErrorCode != null && (ex.ErrorCode.Equals("InvalidAccessKeyId") || ex.ErrorCode.Equals("InvalidSecurity"))) {
                    updater.AddModelError("Settings", T("Invalid AWS credentials"));
                }
                else if (ex.ErrorCode != null && ex.ErrorCode.Equals("AccessDenied")) {
                    updater.AddModelError("Settings", T("Access denied. You don't have permission to access the bucket '{0}'", part.Record.BucketName));
                }
                else {
                    updater.AddModelError("Settings", T("Unknown error: {0}", ex.Message));
                }
            }
            finally {
                if (client != null)
                    client.Dispose();
            }
        }
    }
}