using System;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

static Random rnd = new Random();
// Connection strings to the azure storage
// The xmasTree is the "common storage" of gifs
const string xmasTreeStorageConnectionString = "you'll get that during the course";

// The stocking is your personal storage for your gift
static const string stockingStorageConnectionString = "you'll paste your own connection string here";

public static Run(TimerInfo myTimer, ILogger log)
{
    // Setup connection to both blob storages
    // Storage accounts for both storages
    var xmasTreeStorageAccount = CloudStorageAccount.Parse(xmasTreeStorageConnectionString);
    var stockingStorageAccount = CloudStorageAccount.Parse(stockingStorageConnectionString);
    // CloudBlobClient instances for working with blobs
    var xmasTreeCloudBlobClient = xmasTreeStorageAccount.CreateCloudBlobClient();
    var stockingCloudBlobClient = stockingStorageAccount.CreateCloudBlobClient();
    // Reference xmastree and stocking containers in appropriate storages
    var xmasTreeCloudBlobContainer = xmasTreeCloudBlobClient.GetContainerReference("xmastree");
    var stockingCloudBlobContainer = stockingCloudBlobClient.GetContainerReference("stocking");

    // List the blobs in the container.
    // Create collection for all source blobs
    var giftList = xmasTreeCloudBlobContainer.ListBlobs().Select(x => x as CloudBlockBlob).ToList();
    // Get a random index in the range [0..Count-1] and get a gift from the list with that index
    int randomIndex = rnd.Next(giftList.Count);
    var randomGift = giftList[randomIndex];

    // The blob for gift in our stocking container
    var stockingGift = stockingCloudBlobContainer.GetBlockBlobReference(randomGift.Name);
    // Copy the gift to our stocking
    // We will copy the blob through memory stream
    using(var memoryStream = new MemoryStream())
    {
        // Download to memory first
        randomGift.DownloadToStream(memoryStream);
        // Reset the stream to upload from the start
        memoryStream.Seek(0, SeekOrigin.Begin);
        // Upload the stream
        stockingGift.UploadFromStream(memoryStream);
    }

    // Once the copying was finished, delete the gift
    randomGift.DeleteIfExists();
}

