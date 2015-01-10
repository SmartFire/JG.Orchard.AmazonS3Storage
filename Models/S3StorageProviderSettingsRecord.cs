using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Amazon;
using Orchard.ContentManagement.Records;

namespace JG.Orchard.AmazonS3Storage.Models
{
    public class JGS3StorageProviderSettingsRecord : ContentPartRecord
    {
        [Required]
        [DisplayName("AWS Access Key")]
        public virtual string AWSAccessKey { get; set; }

        [Required]
        [DisplayName("AWS Secret Key")]
        [PasswordPropertyText]
        [System.ComponentModel.DataAnnotations.DataType(DataType.Password)]
        public virtual string AWSSecretKey { get; set; }

        [Required]
        [DisplayName("S3 Bucket Name")]
        public virtual string BucketName { get; set; }

        [Required]
        [DisplayName("S3 Region Endpoint")]
        public string RegionEndpoint { get; set; }

        public IEnumerable<S3EndpointInfo> GetEndpoints() {
            return Amazon.RegionEndpoint.EnumerableAllRegions.Select(ep => new S3EndpointInfo() {DisplayName = ep.DisplayName, SystemName = ep.SystemName});
        }
    }

    public class S3EndpointInfo {
        public string DisplayName { get; set; }
        public string SystemName { get; set; }
    }
}