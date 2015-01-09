using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
    }
}