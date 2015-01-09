using System;
using System.Linq;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using JG.Orchard.AmazonS3Storage.Models;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.Localization;

namespace JG.Orchard.AmazonS3Storage.Drivers
{
    public class S3StorageProviderSettingsPartDriver : ContentPartDriver<S3StorageProviderSettingsPart> {
        public S3StorageProviderSettingsPartDriver()
        {
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        protected override string Prefix { get { return "S3StorageProviderSettings"; } }

        protected override DriverResult Editor(S3StorageProviderSettingsPart part, dynamic shapeHelper)
        {
            return Editor(part, null, shapeHelper);
        }

        protected override DriverResult Editor(S3StorageProviderSettingsPart part, IUpdateModel updater, dynamic shapeHelper)
        {

            return ContentShape("Parts_AmazonS3Media_SiteSettings", () =>
            {
                    if (updater != null && updater.TryUpdateModel(part, Prefix, null, null)) {
                        ValidateS3Connection(part, updater);
                    }
                    return shapeHelper.EditorTemplate(TemplateName: "Parts.AmazonS3Media.SiteSettings", Model: part.Record, Prefix: Prefix); 
                })
                .OnGroup("Amazon S3");
        }

        private void ValidateS3Connection(S3StorageProviderSettingsPart part, IUpdateModel updater) 
        {
            var s3Config = new AmazonS3Config() {
                RegionEndpoint = RegionEndpoint.GetBySystemName(part.Record.RegionEndpoint),
                UseHttp = true
            };

            try {
                // Check AWS credentials, bucket name and bucket permissions
                string bucketName = part.Record.BucketName;

                using (var client = Amazon.AWSClientFactory.CreateAmazonS3Client(part.Record.AWSAccessKey, part.Record.AWSSecretKey, s3Config)) {

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


                    if (!AmazonS3Util.DoesS3BucketExist(client, bucketName))
                    {
                        updater.AddModelError("Settings", T("Invalid bucket name. No bucket by the name {0} exists.", part.Record.BucketName));
                    } else {
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
                }
            } catch (AmazonS3Exception ex) {
                if (ex.ErrorCode != null && (ex.ErrorCode.Equals("InvalidAccessKeyId") || ex.ErrorCode.Equals("InvalidSecurity"))) {
                    updater.AddModelError("Settings", T("Invalid AWS credentials"));
                } else if (ex.ErrorCode != null && ex.ErrorCode.Equals("AccessDenied")) {
                    updater.AddModelError("Settings", T("Access denied. You don't have permission to access the bucket '{0}'", part.Record.BucketName));
                } else {
                    updater.AddModelError("Settings", T("Unknown error: {0}", ex.Message));
                }
            }
        }
    }
}