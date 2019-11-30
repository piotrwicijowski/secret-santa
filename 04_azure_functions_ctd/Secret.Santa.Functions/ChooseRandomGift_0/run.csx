using System;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

// Connection strings to the azure storage
// The xmasTree is the "common storage" of gifs
const string xmasTreeStorageConnectionString = "you'll get that during the course";
// The stocking is your personal storage for your gift
const string stockingStorageConnectionString = "you'll paste your own connection string here";

public static Run(TimerInfo myTimer, ILogger log)
{
    // 1. and 2. - Setup connection to both blob storages
    // Storage accounts for both storages
    var xmasTreeStorageAccount = ...
    var stockingStorageAccount = ...
    // CloudBlobClient instances for working with blobs
    var xmasTreeCloudBlobClient = ...
    var stockingCloudBlobClient = ...
    // Reference xmastree and stocking containers in appropriate storages
    var xmasTreeCloudBlobContainer = ...
    var stockingCloudBlobContainer = ...

    // 3. List the blobs in the container.
    // Create collection for all source blobs
    var giftList = xmasTreeCloudBlobContainer....
    // 4. Pick random gift
    var randomGift = giftList....

    // 5. and 6. "Move" the gift to the stocking
    var ourGift = stockingCloudBlobContainer...
    ourGift....Upload/Copy/Move....randomGift....

}

