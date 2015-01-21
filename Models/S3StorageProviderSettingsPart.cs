using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Amazon.S3.Model;
using Orchard.ContentManagement;

namespace JG.Orchard.AmazonS3Storage.Models
{
    public class S3StorageProviderSettingsPart : ContentPart<JGS3StorageProviderSettingsRecord>
    {
        [DisplayName("AWS Access Key")]
        public virtual string AWSAccessKey
        {
            get { return Record.AWSAccessKey; }
            set { Record.AWSAccessKey = value; }
        }

        [DisplayName("AWS Secret Key")]
        public virtual string AWSSecretKey
        {
            get { return Record.AWSSecretKey; }
            set { Record.AWSSecretKey = value; }
        }

        [Required]
        [DisplayName("S3 Bucket Name")]
        public virtual string BucketName
        {
            get { return Record.BucketName; }
            set { Record.BucketName = value; }
        }

        [DisplayName("S3 Region Endpoint")]
        public virtual string RegionEndpoint
        {
            get { return Record.RegionEndpoint; }
            set { Record.RegionEndpoint = value; }
        }

        [Required]
        [DisplayName("Use IAM Role")]
        public virtual bool UseCustomCredentials {
            get { return Record.UseCustomCredentials; }
            set { Record.UseCustomCredentials = value; }
        }

        public virtual IEnumerable<S3RegionEndpoint> AvailableEndpoints {
            get { return Amazon.RegionEndpoint.EnumerableAllRegions
                .Select(ep=>new S3RegionEndpoint(){DisplayName=ep.DisplayName, SystemName = ep.SystemName}); }
        }
    }

    public class S3RegionEndpoint {
        public string SystemName { get; set; }
        public string DisplayName { get; set; }
    }
}