using JG.Orchard.AmazonS3Storage.Models;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using Orchard.Localization;

namespace JG.Orchard.AmazonS3Storage.Handlers {
    public class S3StorageProviderSettingsPartHandler : ContentHandler {
        public S3StorageProviderSettingsPartHandler(IRepository<JGS3StorageProviderSettingsRecord> repository)
        {
            T = NullLocalizer.Instance;
            Filters.Add(new ActivatingFilter<S3StorageProviderSettingsPart>("Site"));
            Filters.Add(StorageFilter.For(repository));
        }

        public Localizer T { get; set; }

        protected override void GetItemMetadata(GetContentItemMetadataContext context) {
            if (context.ContentItem.ContentType != "Site")
                return;
            base.GetItemMetadata(context);
            context.Metadata.EditorGroupInfo.Add(new GroupInfo(T("Amazon S3")));
        }
    }
}