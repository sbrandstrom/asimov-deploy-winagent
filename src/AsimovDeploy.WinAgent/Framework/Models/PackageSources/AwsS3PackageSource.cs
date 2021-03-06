﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace AsimovDeploy.WinAgent.Framework.Models.PackageSources
{
    public class AwsS3PackageSource : PackageSource
    {
        private AmazonS3Client _s3Client;
        private AmazonS3Client S3Client => _s3Client ?? (_s3Client = new AmazonS3Client(RegionEndpoint.GetBySystemName(Region)));

        public string Region { get; set; }
        public string Bucket { get; set; }
        public string Prefix { get; set; }
        public string Pattern { get; set; } = AsimovVersion.DefaultPattern;


        public override IList<AsimovVersion> GetAvailableVersions(PackageInfo packageInfo)
        {
            var prefix = packageInfo.SourceRelativePath != null ? 
                $"{Prefix}/{packageInfo.SourceRelativePath}" :
                Prefix;
            var objects = S3Client.ListObjects(Bucket, prefix);
            return objects.S3Objects.Select(x => ParseVersion(x.Key, x.LastModified)).Where(x => x != null).ToList();
        }

        private AsimovVersion ParseVersion(string key, DateTime xLastModified)
        {
            return AsimovVersion.Parse(Pattern,key,xLastModified);
        }

        public override AsimovVersion GetVersion(string versionId, PackageInfo packageInfo)
        {
            var @object = S3Client.GetObject(Bucket, versionId);
            return ParseVersion(@object.Key, @object.LastModified);
        }

        public override string CopyAndExtractToTempFolder(string versionId, PackageInfo packageInfo, string tempFolder)
        {
            var @object = S3Client.GetObject(Bucket, versionId);
            var objectFileName = Path.GetFileName(@object.Key);
            if (objectFileName == null)
            {
                throw new InvalidOperationException($"Could not extract file name from object key {@object.Key}");
            }
            var localFileName = Path.Combine(tempFolder, objectFileName);
            @object.WriteResponseStreamToFile(localFileName);

            Extract(localFileName, tempFolder, packageInfo.InternalPath);

            File.Delete(localFileName);

            return Path.Combine(tempFolder, packageInfo.InternalPath);
        }
    }
}
