using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

static Random rnd = new Random();
// Connection strings to the azure storage
// The xmasTree is the "common storage" of gifs
const string xmasTreeStorageConnectionString = "you'll get that during the course";

// The stocking is your personal storage for your gift
static const string stockingStorageConnectionString = "you'll paste your own connection string here";

public async static Task Run(TimerInfo myTimer, ILogger log)
{
    log.LogInformation("Setting up storage accounts, blob clients and containers");
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
    log.LogInformation("Getting a list of all gifts from xmastree container");
    var giftList = xmasTreeCloudBlobContainer.ListBlobs().Select(x => x as CloudBlockBlob).ToList();
    if(giftList.Count < 1)
    {
        log.LogInformation("There are no gifts under the tree to chose from");
        return;
    }
    log.LogInformation($"All available gifts in xmastree container:\n{string.Join("\n",giftList.Select(x => x.Name))}");
    // Get a random index in the range [0..Count-1] and get a gift from the list with that index
    int randomIndex = rnd.Next(giftList.Count);
    var randomGift = giftList[randomIndex];

    log.LogInformation($"My random gift is {randomGift.Name}");

    // The blob for gift in our stocking container
    var stockingGift = stockingCloudBlobContainer.GetBlockBlobReference(randomGift.Name);
    // Copy the gift to our stocking
    log.LogInformation($"Copying the gift {randomGift.Name} to our container");
    // We will copy the blob through memory stream
    using(var memoryStream = new MemoryStream())
    {
        // Download to memory first
        await randomGift.DownloadToStreamAsync(memoryStream);
        // Reste the stream to upload from the start
        memoryStream.Seek(0, SeekOrigin.Begin);
        // Upload the stream
        await stockingGift.UploadFromStreamAsync(memoryStream);
        // Set the content type to the original one (presumably "image/jpeg" or similar)
        stockingGift.Properties.ContentType = randomGift.Properties.ContentType;
        stockingGift.SetProperties();
    }

    // Once the copying was finished, delete the gift
    log.LogInformation($"Deleting the gift {randomGift.Name} from xmastree container");
    await randomGift.DeleteIfExistsAsync();
    log.LogInformation("Finished");
}

