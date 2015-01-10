using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Orchard.ContentManagement;

namespace JG.Orchard.AmazonS3Storage.Models
{
    public class S3StorageProviderSettingsPart : ContentPart<JGS3StorageProviderSettingsRecord>
    {
        [Required]
        [DisplayName("AWS Access Key")]
        public virtual string AWSAccessKey
        {
            get { return Record.AWSAccessKey; }
            set { Record.AWSAccessKey = value; }
        }

        [Required]
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

        [Required]
        [DisplayName("S3 Region Endpoint")]
        public virtual string RegionEndpoint
        {
            get { return Record.RegionEndpoint; }
            set { Record.RegionEndpoint = value; }
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