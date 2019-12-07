using System;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

// Connection strings for both Storage Accounts:
// The xmastree is the "common storage" of gifts
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
    var xmasTreeCloudBlobContainer = xmasTreeCloudBlobClient.GetContainerReference("xmastree");
    var stockingCloudBlobContainer = stockingCloudBlobClient.GetContainerReference("stocking");

    // 3. List the blobs in the container.
    var giftList = xmasTreeCloudBlobContainer.ListBlobs().Select(x => x as CloudBlockBlob).ToList();
    // 4. Pick random gift
    // Randomize the order in which we will try picking up the presents
    var rnd = new Random();
    var randomizedGifts = giftList.OrderBy(g => rnd.Next()).ToList();
    // Blob leasing - making sure only one client at a time can access a specific blob
    // Variable that will eventually hold a gift that we managed to get a lease on
    CloudBlockBlob randomGift = null;
    string leaseID = null;
    AccessCondition acc = null;
    // Maximum finite time for lease is 60 seconds
    TimeSpan leaseTime = TimeSpan.FromSeconds(60);
    // Check each gift one by one and try to lease it - if someone was faster, move to another one
    foreach(var possibleGift in randomizedGifts)
    {
        try
        {
            // Try to acquire lease - if someone was faster, this method throws an exception
            leaseID = possibleGift.AcquireLease(leaseTime, null);
            // If acquiring a lease was successful, do the following
            log.LogInformation($"Leasing successful {possibleGift.Name}");
            // AccessCondition is needed for operating on a leased blob
            acc = new AccessCondition();
            acc.LeaseId = leaseID;
            // The current gift was leased, don't check any more possible gifts
            randomGift = possibleGift;
            break;
        }
        catch(Exception)
        {
            log.LogInformation($"The gift {possibleGift.Name} was already leased, trying next");
        }
    }
    // If we tried to lease all gifts and none of it was available, return
    if(randomGift == null){
        log.LogError($"All gifts were already leased, exiting");
        return;
    }

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
    randomGift.DeleteIfExists(DeleteSnapshotsOption.IncludeSnapshots, acc, null, null);
}

