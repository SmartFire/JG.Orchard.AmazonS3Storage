using System;
using System.IO;
using Amazon.S3.IO;
using JG.Orchard.AmazonS3Storage.Providers;
using Orchard.FileSystems.Media;

namespace JG.Orchard.AmazonS3Storage.Models
{
    public class S3StorageFile : IStorageFile
    {
        private readonly S3FileInfo _s3FileInfo;
        private readonly S3StorageProvider _s3StorageProvider;

        public S3StorageFile() {
            
        }

        public S3StorageFile(S3FileInfo s3FileInfo, S3StorageProvider s3StorageProvider) {
            _s3FileInfo = s3FileInfo;
            _s3StorageProvider = s3StorageProvider;
        }

        public string GetPath()
        {
            return _s3FileInfo.Name;
        }

        public string GetName()
        {
            return Path.GetFileName(_s3FileInfo.Name);
        }

        public long GetSize()
        {
            return 0;
        }

        public DateTime GetLastUpdated()
        {
            return DateTime.Now;
        }

        public string GetFileType()
        {
            return _s3FileInfo.GetType().Name;
        }

        public virtual Stream OpenRead()
        {
            return _s3FileInfo.OpenRead();
        }


        public Stream OpenWrite() {
            return new DelayedS3Writer(this, _s3FileInfo.OpenWrite(), "public-read", _s3StorageProvider);
        }

        public Stream CreateFile()
        {

            return _s3FileInfo.Create();
        }
    }

    public class DelayedS3Writer : Stream, IDisposable {
        private readonly S3StorageFile _file;
        private Stream _innerStream = null;
        public S3StorageProvider _storageProvider;
        private readonly string _cannedAcl;
        private MemoryStream _buffer = null;

        public DelayedS3Writer(S3StorageFile file, Stream innerStream, string cannedAcl, S3StorageProvider s3StorageProvider) {
            _file = file;
            _innerStream = innerStream;
            _storageProvider = s3StorageProvider;
            _cannedAcl = cannedAcl;
            _buffer = new MemoryStream();
        }

        

        public void Dispose() {
            _buffer.Seek(0, SeekOrigin.Begin);
            _buffer.CopyTo(_innerStream);
            _innerStream.Flush();
            _innerStream.Close();
            _innerStream.Dispose();
            _buffer.Flush();
            _buffer.Dispose();

            _storageProvider.SetFileAcl(_file.GetPath());
        }

        public override void Flush() {
            _buffer.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin) {
            return _buffer.Seek(offset, origin);
        }

        public override void SetLength(long value) {
            _buffer.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count) {
            return _buffer.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count) {
            _buffer.Write(buffer, offset, count);
        }

        public override bool CanRead {
            get { return _buffer.CanRead; }
        }

        public override bool CanSeek {
            get { return _buffer.CanSeek; }
        }

        public override bool CanWrite {
            get { return _buffer.CanWrite; }
        }

        public override long Length {
            get { return _buffer.Length; }
        }

        public override long Position { get; set; }
    }
}
