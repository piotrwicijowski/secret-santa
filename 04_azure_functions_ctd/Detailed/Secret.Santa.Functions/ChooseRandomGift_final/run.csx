using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

static Random rnd = new Random();
// Connection strings to the azure storage
// The xmasTree is the "common storage" of gifs
const string xmasTreeContainerUrl = "you'll get that during the course";
const string xmasTreeSASToken = "you'll get that during the course";
// The stocking is your personal storage for your gift
const string stockingStorageConnectionString = "you'll paste your own connection string here";

public async static Task Run(TimerInfo myTimer, ILogger log)
{
    log.LogInformation("Setting up storage accounts, blob clients and containers");
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
    log.LogInformation($"Randomized gifts:\n{string.Join("\n",randomizedGifts.Select(x => x.Name))}");

    // Blob leasing - making sure only one client at a time can access a specific blob
    // Variable that will eventually hold a gift that we managed to get a lease on
    CloudBlockBlob leasedGift = null;
    string leaseID = null;
    AccessCondition acc = null;
    // Maximum finite time for lease is 60 seconds
    TimeSpan leaseTime = TimeSpan.FromSeconds(60);
    // For continuous renewing a lease in a background Task we need a way to cancel the renewal


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

    CancellationTokenSource tokenSource = new CancellationTokenSource();
    CancellationToken token = tokenSource.Token;
    try
    {
        // Define a background task that will renew the lease periodically (in case the download takes more than 60s)
        Task.Run(() =>
        {
            try
            {
                // Do not try renewing the lease infinitely, 10 times should be sufficient
                for (int i = 0; i < 10; i++)
                {
                    // Renew the lease every 50 seconds
                    log.LogInformation("Waiting 50s for lease renewal");
                    Thread.Sleep(TimeSpan.FromSeconds(50));
                    // Check if renewing was cancelled - if so, throw an Exception
                    token.ThrowIfCancellationRequested();
                    // Renew the lease
                    log.LogInformation("Renewing lease");
                    leasedGift.RenewLease(acc);
                }
            }
            catch(OperationCanceledException)
            {
                log.LogInformation("Lease renewal canceled");
            }
        }, token);

        log.LogInformation($"My random gift is {leasedGift.Name}");

        // Copying blobs between two storage accounts (without downloading it to memory first) 
        // Require the use of SAS token, which we already have
        log.LogInformation("Combinding the leased gift Url with SAS token for access");
        var leasedGiftUrl = leasedGift.Uri.AbsoluteUri + xmasTreeSASToken;

        // The blob for gift in our stocking container
        var stockingGift = stockingCloudBlobContainer.GetBlockBlobReference(leasedGift.Name);
        // Copy the leased gift to our stocking
        log.LogInformation($"Copying the leased gift {leasedGift.Name} to our container");
        await stockingGift.StartCopyAsync(new Uri(leasedGiftUrl));

        while (stockingGift.CopyState.Status == CopyStatus.Pending)
        {
            await Task.Delay(200);
            await stockingGift.FetchAttributesAsync();
        }

        // Once the copying was finished, delete the leased gift
        // Since the blob was leased, in order to remove it, we need to specify
        // the lease (variable acc)
        log.LogInformation($"Deleting the leased gift {leasedGift.Name} from xmastree container");
        await leasedGift.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, acc, null, null);
        // We no longer need to have the token renewed in background Task

    }
    finally
    {
        tokenSource.Cancel();
        log.LogInformation("Finished");
    }
}

