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
    // Randomize the order in which we will try picking up the presents
    var randomizedGifts = giftList.OrderBy(g => rnd.Next()).ToList();
    log.LogInformation($"Randomized gifts:\n{string.Join("\n",giftList.Select(x => x.Name))}");

    // Blob leasing - making sure only one client at a time can access a specific blob
    // Variable that will eventually hold a gift that we managed to get a lease on
    CloudBlockBlob leasedGift = null;
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
            log.LogInformation($"Trying to lease {possibleGift.Name}");
            leaseID = possibleGift.AcquireLease(leaseTime, null);

            // If acquiring a lease was successful, do the following
            log.LogInformation($"Leasing successful {possibleGift.Name}");
            // AccessCondition is needed for operating on a leased blob
            acc = new AccessCondition();
            acc.LeaseId = leaseID;
            // The current gift was leased, don't check any more possible gifts
            leasedGift = possibleGift;
            break;
        }
        catch(Exception)
        {
            log.LogInformation($"The gift {possibleGift.Name} was already leased, trying next");
        }
    }
    // If we tried to lease all gifts and none of it was available, return
    if(leasedGift == null){
        log.LogError($"All gifts were already leased, exiting");
        return;
    }
    log.LogInformation($"My random gift is {leasedGift.Name}");

    // Copying blobs between two storage accounts (without downloading it to memory first) require a bit of trickery
    // For that we need to create a Shared Access Signature
    log.LogInformation("Setting up Shared Access Signature");
    var toDateTime = DateTime.Now.AddMinutes(60);
    var policy = new SharedAccessBlobPolicy
    {
        Permissions = SharedAccessBlobPermissions.Read |
        SharedAccessBlobPermissions.Write |
        SharedAccessBlobPermissions.Delete,
        SharedAccessStartTime = null,
        SharedAccessExpiryTime = new DateTimeOffset(toDateTime)
    };

    var sas = leasedGift.GetSharedAccessSignature(policy);
    var leasedGiftUrl = leasedGift.Uri.AbsoluteUri + sas;

    // The blob for gift in our stocking container
    var stockingGift = stockingCloudBlobContainer.GetBlockBlobReference(leasedGift.Name);
    // Copy the leased gift to our stocking
    log.LogInformation($"Copying the leased gift {leasedGift.Name} to our container");
    await stockingGift.StartCopyAsync(new Uri(leasedGiftUrl));

    // Once the copying was finished, delete the leased gift
    // Since the blob was leased, in order to remove it, we need to specify
    // the lease (variable acc)
    log.LogInformation($"Deleting the leased gift {leasedGift.Name} from xmastree container");
    await leasedGift.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, acc, null, null);
    log.LogInformation("Finished");
}

