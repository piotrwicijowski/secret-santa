using System;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

// SAS Uri for the xmastree container
// The xmastree is the "common storage" of gifts
const string xmasTreeContainerUrl = "you'll get that during the course";
const string xmasTreeSASToken = "you'll get that during the course";
// The stocking is your personal storage for your gift
const string stockingStorageConnectionString = "you'll paste your own connection string here";

public static void Run(TimerInfo myTimer, ILogger log)
{
    // 1. and 2. - Setup connection to both blob storages
    // Storage accounts for your storage
    var stockingStorageAccount = ...
    // CloudBlobClient instance for working with blobs
    var stockingCloudBlobClient = ...
    // Reference xmastree and stocking containers
    var xmasTreeCloudBlobContainer = ... //use the SAS Uri
    var stockingCloudBlobContainer = ... //use the Client

    // 3. List the blobs in the container.
    // Create collection for all source blobs
    var giftList = xmasTreeCloudBlobContainer....
    // 4. Pick random gift
    var randomGift = giftList....

    // 5. and 6. "Move" the gift to the stocking
    var ourGift = stockingCloudBlobContainer...
    ourGift....Upload/Copy/Move....randomGift....

}

