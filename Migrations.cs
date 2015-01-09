using Orchard.Data.Migration;

namespace JG.Orchard.AmazonS3Storage
{
    public class Migrations : DataMigrationImpl
    {

        public int Create()
        {
            SchemaBuilder.CreateTable("JGS3StorageProviderSettingsRecord", table => table
                .ContentPartRecord()
                .Column<string>("AWSAccessKey")
                .Column<string>("AWSSecretKey")
                .Column<string>("RegionEndpoint")
                .Column<string>("BucketName")
               );

            return 1;
        }
    }
}
