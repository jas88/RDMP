// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.
using NUnit.Framework;
using Amazon.S3;
using Amazon.S3.Model;
using Rdmp.Core.ReusableLibraryCode.AWS;
using Tests.Common.Scenarios;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.ReusableLibraryCode.Progress;
using System;
using Rdmp.Core.Curation.Data.Pipelines;
using Rdmp.Core.DataExport.DataRelease;
using Rdmp.Core.CommandLine.Options;
using Rdmp.Core.CommandLine.Runners;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.ReusableLibraryCode.Checks;
using System.Linq;
using System.Collections.Generic;
using Amazon;
using Rdmp.Core.Curation.Data.DataLoad;

namespace Rdmp.Core.Tests.DataExport.DataRelease;

public sealed class S3BucketReleaseDestinationTests : TestsRequiringAnExtractionConfiguration
{
    private const string Username = "minioadmin";
    private const string Password = "minioadmin";
    private const string Endpoint = "127.0.0.1:9000";
    private static IAmazonS3 _s3Client;


    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _s3Client?.Dispose();
    }

    [OneTimeSetUp]
    public new void OneTimeSetUp()
    {
        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.USEast1, // MinIO default
            ServiceURL = $"http://{Endpoint}",
            ForcePathStyle = true // Required for MinIO compatibility
        };

        _s3Client = new AmazonS3Client(Username, Password, config);
    }

    private void DoExtraction()
    {
        SetUp();
        Execute(out _, out _, ThrowImmediatelyDataLoadEventListener.Quiet);
    }

    private static void MakeBucket(string name)
    {
        var request = new PutBucketRequest
        {
            BucketName = name,
            UseClientRegion = true
        };
        _s3Client.PutBucketAsync(request).Wait();
    }

    private static void DeleteBucket(string name)
    {
        var request = new DeleteBucketRequest
        {
            BucketName = name
        };
        _s3Client.DeleteBucketAsync(request).Wait();
    }

    private static void DeleteBucketAndContents(string name)
    {
        // First, delete all objects in the bucket
        var objects = GetObjects(name);
        foreach (var obj in objects)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = name,
                Key = obj.Key
            };
            _s3Client.DeleteObjectAsync(request).Wait();
        }

        // Now delete the empty bucket
        DeleteBucket(name);
    }

    private static List<S3Object> GetObjects(string bucketName)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = bucketName
        };
        var response = _s3Client.ListObjectsV2Async(request).Result;

        // Filter out directory markers and empty objects to match MinIO client behavior
        return response.S3Objects
            .Where(obj => !obj.Key.EndsWith("/")) // Exclude directory markers
            .Where(obj => obj.Size > 0) // Exclude empty marker objects
            .ToList();
    }

    private static void SetArgs(IArgument[] args, Dictionary<string, object> values)
    {
        foreach (var x in args)
        {
            if (!values.TryGetValue(x.Name, out var value) || x.GetValueAsSystemType()?.Equals(value) == true) continue;

            x.SetValue(value);
            x.SaveToDatabase();
        }
    }

    [Test]
    public void AWSLoginTest()
    {
        var awss3 = new AWSS3("minio", Amazon.RegionEndpoint.EUWest2);
        Assert.DoesNotThrow(() => MakeBucket("logintest"));
        Assert.DoesNotThrow(() => DeleteBucket("logintest"));
    }

    [Test]
    public void ReleaseToAWSBasicTest()
    {
        MakeBucket("releasetoawsbasictest");
        DoExtraction();
        var pipe = new Pipeline(CatalogueRepository, "NestedPipe1");
        var pc = new PipelineComponent(CatalogueRepository, pipe, typeof(AWSS3BucketReleaseDestination), -1,
            "AWS S3 Release");
        pc.SaveToDatabase();

        var args = pc.CreateArgumentsForClassIfNotExists<AWSS3BucketReleaseDestination>();

        Assert.That(pc.GetAllArguments().Any());

        SetArgs(args, new Dictionary<string, object>
        {
            { "AWS_Profile", "minio" },
            { "BucketName", "releasetoawsbasictest" },
            { "AWS_Region", "eu-west-2" },
            { "ConfigureInteractivelyOnRelease", false },
            { "BucketFolder", "release" }
        });

        pipe.DestinationPipelineComponent_ID = pc.ID;
        pipe.SaveToDatabase();
        var optsRelease = new ReleaseOptions
        {
            Configurations = _configuration.ID.ToString(),
            Pipeline = pipe.ID.ToString()
        };
        var runner = new ReleaseRunner(new ThrowImmediatelyActivator(RepositoryLocator), optsRelease);
        Assert.DoesNotThrow(() => runner.Run(RepositoryLocator, ThrowImmediatelyDataLoadEventListener.Quiet, ThrowImmediatelyCheckNotifier.Quiet, new GracefulCancellationToken()));
        var foundObjects = GetObjects("releasetoawsbasictest");
        Assert.That(foundObjects, Has.Count.EqualTo(1));

        // Clean up bucket and its contents after test
        DeleteBucketAndContents("releasetoawsbasictest");
    }

    [Test]
    public void NoRegion()
    {
        DoExtraction();
        var pipe = new Pipeline(CatalogueRepository, "NestedPipe2");
        var pc = new PipelineComponent(CatalogueRepository, pipe, typeof(AWSS3BucketReleaseDestination), -1,
            "AWS S3 Release");
        pc.SaveToDatabase();

        var args = pc.CreateArgumentsForClassIfNotExists<AWSS3BucketReleaseDestination>();

        Assert.That(pc.GetAllArguments().Any());

        SetArgs(args, new Dictionary<string, object>
        {
            { "AWS_Profile", "minio" },
            { "BucketName", "noregion" },
            { "ConfigureInteractivelyOnRelease", false },
            { "BucketFolder", "release" }
        });

        pipe.DestinationPipelineComponent_ID = pc.ID;
        pipe.SaveToDatabase();
        var optsRelease = new ReleaseOptions
        {
            Configurations = _configuration.ID.ToString(),
            Pipeline = pipe.ID.ToString(),
            Command = CommandLineActivity.check
        };
        var runner = new ReleaseRunner(new ThrowImmediatelyActivator(RepositoryLocator), optsRelease);
        Assert.Throws<AggregateException>(() => runner.Run(RepositoryLocator, ThrowImmediatelyDataLoadEventListener.Quiet, ThrowImmediatelyCheckNotifier.Quiet, new GracefulCancellationToken()));
    }

    [Test]
    public void NoProfile()
    {
        DoExtraction();
        var pipe = new Pipeline(CatalogueRepository, "NestedPipe3");
        var pc = new PipelineComponent(CatalogueRepository, pipe, typeof(AWSS3BucketReleaseDestination), -1,
            "AWS S3 Release");
        pc.SaveToDatabase();

        var args = pc.CreateArgumentsForClassIfNotExists<AWSS3BucketReleaseDestination>();

        Assert.That(pc.GetAllArguments().Any());

        SetArgs(args, new Dictionary<string, object>
        {
            { "AWS_Region", "eu-west-2" },
            { "BucketName", "noprofile" },
            { "ConfigureInteractivelyOnRelease", false },
            { "BucketFolder", "release" }
        });

        pipe.DestinationPipelineComponent_ID = pc.ID;
        pipe.SaveToDatabase();
        var optsRelease = new ReleaseOptions
        {
            Configurations = _configuration.ID.ToString(),
            Pipeline = pipe.ID.ToString(),
            Command = CommandLineActivity.check
        };
        var runner = new ReleaseRunner(new ThrowImmediatelyActivator(RepositoryLocator), optsRelease);
        Assert.Throws<AggregateException>(() => runner.Run(RepositoryLocator, ThrowImmediatelyDataLoadEventListener.Quiet, ThrowImmediatelyCheckNotifier.Quiet, new GracefulCancellationToken()));
    }

    [Test]
    public void BadProfile()
    {
        DoExtraction();
        var pipe = new Pipeline(CatalogueRepository, "NestedPipe4");
        var pc = new PipelineComponent(CatalogueRepository, pipe, typeof(AWSS3BucketReleaseDestination), -1,
            "AWS S3 Release");
        pc.SaveToDatabase();

        var args = pc.CreateArgumentsForClassIfNotExists<AWSS3BucketReleaseDestination>();

        Assert.That(pc.GetAllArguments().Any());

        SetArgs(args, new Dictionary<string, object>
        {
            { "AWS_Region", "eu-west-2" },
            { "AWS_Profile", "junk-profile" },
            { "BucketName", "badprofile" },
            { "ConfigureInteractivelyOnRelease", false },
            { "BucketFolder", "release" }
        });

        pipe.DestinationPipelineComponent_ID = pc.ID;
        pipe.SaveToDatabase();
        var optsRelease = new ReleaseOptions
        {
            Configurations = _configuration.ID.ToString(),
            Pipeline = pipe.ID.ToString(),
            Command = CommandLineActivity.check
        };
        var runner = new ReleaseRunner(new ThrowImmediatelyActivator(RepositoryLocator), optsRelease);
        Assert.Throws<AggregateException>(() => runner.Run(RepositoryLocator, ThrowImmediatelyDataLoadEventListener.Quiet, ThrowImmediatelyCheckNotifier.Quiet, new GracefulCancellationToken()));
    }


    [Test]
    public void NoBucket()
    {
        DoExtraction();
        var pipe = new Pipeline(CatalogueRepository, "NestedPipe5");
        var pc = new PipelineComponent(CatalogueRepository, pipe, typeof(AWSS3BucketReleaseDestination), -1,
            "AWS S3 Release");
        pc.SaveToDatabase();

        var args = pc.CreateArgumentsForClassIfNotExists<AWSS3BucketReleaseDestination>();

        Assert.That(pc.GetAllArguments().Any());
        SetArgs(args, new Dictionary<string, object>
        {
            { "AWS_Region", "eu-west-2" },
            { "AWS_Profile", "minio" },
            { "ConfigureInteractivelyOnRelease", false },
            { "BucketFolder", "release" }
        });

        pipe.DestinationPipelineComponent_ID = pc.ID;
        pipe.SaveToDatabase();
        var optsRelease = new ReleaseOptions
        {
            Configurations = _configuration.ID.ToString(),
            Pipeline = pipe.ID.ToString(),
            Command = CommandLineActivity.check
        };
        var runner = new ReleaseRunner(new ThrowImmediatelyActivator(RepositoryLocator), optsRelease);
        Assert.Throws<AggregateException>(() => runner.Run(RepositoryLocator, ThrowImmediatelyDataLoadEventListener.Quiet, ThrowImmediatelyCheckNotifier.Quiet, new GracefulCancellationToken()));
    }

    [Test]
    public void BadBucket()
    {
        DoExtraction();
        var pipe = new Pipeline(CatalogueRepository, "NestedPipe6");
        var pc = new PipelineComponent(CatalogueRepository, pipe, typeof(AWSS3BucketReleaseDestination), -1,
            "AWS S3 Release");
        pc.SaveToDatabase();

        var args = pc.CreateArgumentsForClassIfNotExists<AWSS3BucketReleaseDestination>();

        Assert.That(pc.GetAllArguments().Any());
        SetArgs(args, new Dictionary<string, object>
        {
            { "AWS_Region", "eu-west-2" },
            { "AWS_Profile", "minio" },
            { "BucketName", "doesNotExist" },
            { "ConfigureInteractivelyOnRelease", false },
            { "BucketFolder", "release" }
        });

        pipe.DestinationPipelineComponent_ID = pc.ID;
        pipe.SaveToDatabase();
        var optsRelease = new ReleaseOptions
        {
            Configurations = _configuration.ID.ToString(),
            Pipeline = pipe.ID.ToString(),
            Command = CommandLineActivity.check
        };
        var runner = new ReleaseRunner(new ThrowImmediatelyActivator(RepositoryLocator), optsRelease);
        Assert.Throws<AggregateException>(() => runner.Run(RepositoryLocator, ThrowImmediatelyDataLoadEventListener.Quiet, ThrowImmediatelyCheckNotifier.Quiet, new GracefulCancellationToken()));
    }


    [Test]
    public void LocationAlreadyExists()
    {
        MakeBucket("locationalreadyexist");

        DoExtraction();
        var pipe = new Pipeline(CatalogueRepository, "NestedPipe7");
        var pc = new PipelineComponent(CatalogueRepository, pipe, typeof(AWSS3BucketReleaseDestination), -1,
            "AWS S3 Release");
        pc.SaveToDatabase();

        var args = pc.CreateArgumentsForClassIfNotExists<AWSS3BucketReleaseDestination>();

        Assert.That(pc.GetAllArguments().Any());
        SetArgs(args, new Dictionary<string, object>
        {
            { "AWS_Region", "eu-west-2" },
            { "AWS_Profile", "minio" },
            { "BucketName", "locationalreadyexist" },
            { "ConfigureInteractivelyOnRelease", false },
            { "BucketFolder", "release" }
        });

        pipe.DestinationPipelineComponent_ID = pc.ID;
        pipe.SaveToDatabase();
        var optsRelease = new ReleaseOptions
        {
            Configurations = _configuration.ID.ToString(),
            Pipeline = pipe.ID.ToString()
        };
        var runner = new ReleaseRunner(new ThrowImmediatelyActivator(RepositoryLocator), optsRelease);
        Assert.DoesNotThrow(() => runner.Run(RepositoryLocator, ThrowImmediatelyDataLoadEventListener.Quiet, ThrowImmediatelyCheckNotifier.Quiet, new GracefulCancellationToken()));
        var foundObjects = GetObjects("locationalreadyexist");
        Assert.That(foundObjects, Has.Count.EqualTo(1));
        DoExtraction();
        pipe = new Pipeline(CatalogueRepository, "NestedPipe8");
        pc = new PipelineComponent(CatalogueRepository, pipe, typeof(AWSS3BucketReleaseDestination), -1,
            "AWS S3 Release");
        pc.SaveToDatabase();

        args = pc.CreateArgumentsForClassIfNotExists<AWSS3BucketReleaseDestination>();

        Assert.That(pc.GetAllArguments().Any());
        SetArgs(args, new Dictionary<string, object>
        {
            { "AWS_Region", "eu-west-2" },
            { "AWS_Profile", "minio" },
            { "BucketName", "locationalreadyexist" },
            { "ConfigureInteractivelyOnRelease", false },
            { "BucketFolder", "release" }
        });

        pipe.DestinationPipelineComponent_ID = pc.ID;
        pipe.SaveToDatabase();
        optsRelease = new ReleaseOptions
        {
            Configurations = _configuration.ID.ToString(),
            Pipeline = pipe.ID.ToString(),
            Command = CommandLineActivity.check
        };
        runner = new ReleaseRunner(new ThrowImmediatelyActivator(RepositoryLocator), optsRelease);
        Assert.Throws<AggregateException>(() => runner.Run(RepositoryLocator, ThrowImmediatelyDataLoadEventListener.Quiet, ThrowImmediatelyCheckNotifier.Quiet, new GracefulCancellationToken()));
        foundObjects = GetObjects("locationalreadyexist");
        Assert.That(foundObjects, Has.Count.EqualTo(1));

        // Clean up bucket and its contents after test
        DeleteBucketAndContents("locationalreadyexist");
    }
}