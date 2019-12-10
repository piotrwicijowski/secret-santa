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
    var xmasTreeStorageAccount = ...
    var stockingStorageAccount = ...
    // CloudBlobClient instances for working with blobs
    var xmasTreeCloudBlobClient = ...
    var stockingCloudBlobClient = ...
    // Reference xmastree and stocking containers
    var xmasTreeCloudBlobContainer = ...
    var stockingCloudBlobContainer = ...

    // 3. List the blobs in the container.
    var giftList = xmasTreeCloudBlobContainer....
    // 4. Pick random gift
    var randomGift = giftList....

    // 5. and 6. "Move" the gift to the stocking
    var ourGift = stockingCloudBlobContainer...
    ourGift....Upload/Copy/Move....randomGift....

}

