using System;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

static Random rnd = new Random();
// SAS Uri for the xmastree container
// The xmastree is the "common storage" of gifts
const string xmasTreeContainerUrl = "you'll get that during the course";
const string xmasTreeSASToken = "you'll get that during the course";
// The stocking is your personal storage for your gift
const string stockingStorageConnectionString = "you'll paste your own connection string here";

public static void Run(TimerInfo myTimer, ILogger log)
{
    // Setup connection to both blob storage
    // Storage accounts for your storage
    var stockingStorageAccount = CloudStorageAccount.Parse(stockingStorageConnectionString);
    // CloudBlobClient instance for working with blobs
    var stockingCloudBlobClient = stockingStorageAccount.CreateCloudBlobClient();
    // Reference xmastree and stocking containers
    var xmasTreeCloudBlobContainer = new CloudBlobContainer(new Uri(xmasTreeContainerUrl + xmasTreeSASToken), null);
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

