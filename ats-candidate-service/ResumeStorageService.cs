using Minio;
using Minio.DataModel.Args;

namespace ats;

public class ResumeStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;

    public ResumeStorageService(IMinioClient minioClient, IConfiguration config)
    {
        _minioClient = minioClient;
        _bucketName = config["MinioSettings:BucketName"] ?? "resumes";
    }

    public async Task UploadResumeAsync(string fileName, Stream fileStream, string contentType)
    {
        
        bool found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName));
        if (!found)
        {
            await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
        }

        await _minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType));
    }

    // public async Task<Stream> GetResumeAsync(string fileName)
    // {
    //     var ms = new MemoryStream();
    //     await _minioClient.GetObjectAsync(new GetObjectArgs()
    //         .WithBucket(_bucketName)
    //         .WithObject(fileName)
    //         .WithCallbackStream(stream => stream.CopyTo(ms)));
    //     ms.Seek(0, SeekOrigin.Begin);
    //     return ms;
    // }
    
    public async Task<Stream> GetFileAsync(string fileName)
    {
        var ms = new MemoryStream();

        var getObjectArgs = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithCallbackStream(stream =>
            {
                stream.CopyTo(ms);
            });

        await _minioClient.GetObjectAsync(getObjectArgs);
        ms.Position = 0; // сброс курсора для отдачи наружу
        return ms;
    }
}