using System;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

// Connection strings for both Storage Accounts:
// The christmastree is the "common storage" of gifts
const string xmastreeStorageConnectionString = "you'll get this during the workshops";
// The stocking is your personal storage for your gift
const string stockingStorageConnectionString = "you'll paste your own connection string here";

public static void Run(TimerInfo myTimer, ILogger log)
{
    // 1. and 2. - Setup connection to both blob storages
    // Storage accounts for your storage
    var xmasTreeStorageAccount = CloudStorageAccount.Parse(xmastreeStorageConnectionString);
    var stockingStorageAccount = CloudStorageAccount.Parse(stockingStorageConnectionString);
    // CloudBlobClient instances for working with blobs
    var xmasTreeCloudBlobClient = xmasTreeStorageAccount.CreateCloudBlobClient();
    var stockingCloudBlobClient = stockingStorageAccount.CreateCloudBlobClient();
    // Reference xmastree and stocking containers
    var xmasTreeCloudBlobContainer = xmasTreeCloudBlobClient.GetContainerReference("christmastree");
    var stockingCloudBlobContainer = stockingCloudBlobClient.GetContainerReference("stocking");

    // 3. List the blobs in the container.
    var giftList = xmasTreeCloudBlobContainer.ListBlobs().Select(x => x as CloudBlockBlob).ToList();
    // 4. Pick random gift
    // Get a random index in the range [0..Count-1] and get a gift from the list with that index
    var rnd = new Random();
    int randomIndex = rnd.Next(giftList.Count);
    var randomGift = giftList[randomIndex];

    // 5. "Move" the gift to the stocking
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

