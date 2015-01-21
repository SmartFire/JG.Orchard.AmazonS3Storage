using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using Amazon;
using Amazon.EC2.Util;
using Orchard.ContentManagement.Records;

namespace JG.Orchard.AmazonS3Storage.Models
{
    public class JGS3StorageProviderSettingsRecord : ContentPartRecord
    {
        [DisplayName("AWS Access Key")]
        public virtual string AWSAccessKey { get; set; }

        [DisplayName("AWS Secret Key")]
        [PasswordPropertyText]
        [System.ComponentModel.DataAnnotations.DataType(DataType.Password)]
        public virtual string AWSSecretKey { get; set; }

        [Required]
        [DisplayName("S3 Bucket Name")]
        public virtual string BucketName { get; set; }

        [DisplayName("S3 Region Endpoint")]
        public string RegionEndpoint { get; set; }

        [Required]
        [DisplayName("Use IAM Role")]
        public bool UseCustomCredentials { get; set; }

        public IEnumerable<S3EndpointInfo> GetEndpoints() {
            return Amazon.RegionEndpoint.EnumerableAllRegions.Select(ep => new S3EndpointInfo() {DisplayName = ep.DisplayName, SystemName = ep.SystemName});
        }

        public string GetIAMRole() {
            if (IsAmazonEC2Instance()) {
                try {
                    if (EC2Metadata.IAMSecurityCredentials.Count == 1) {
                        var iamRole = EC2Metadata.IAMSecurityCredentials.Keys.First();
                        var credential = EC2Metadata.IAMSecurityCredentials[iamRole];
                        if (credential.Code == "Success")
                            return iamRole;
                    }
                }
                catch {
                    return null;
                }
            }
            return null;
        }

        private bool IsAmazonEC2Instance() {
            string URI = "http://169.254.169.254/latest/meta-data/";
            HttpWebRequest req = (HttpWebRequest) WebRequest.Create(URI);
            req.Method = WebRequestMethods.Http.Head;
            req.Timeout = 500;
            try {
                using (var response = req.GetResponse()) {
                    int TotalSize = Int32.Parse(response.Headers["Content-Length"]);
                    return TotalSize != 0;
                }
            }
            catch (WebException exception) {
                return false;
            }
        }

    }

    public class S3EndpointInfo {
        public string DisplayName { get; set; }
        public string SystemName { get; set; }
    }
}