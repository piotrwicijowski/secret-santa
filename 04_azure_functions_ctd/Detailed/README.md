# Azure Functions continued (Detailed version)

This is the "I sit at home and I'll do everything in my own time" tutorial instructions - they are more read-heavy and more detailed than the [workshops version](../README.md). If you are on workshops right now, we highly recommend to open [workshops version](../README.md) instead.

## Introduction
In the previous module we have used the Azure Functions in order to send an image to a common blob storage container. That action was triggered from a web page. In the current module we will continue our journey with Azure Functions, but we will look at them from a bit different angle.

First of all, we will be working on Functions that are executed on a schedule with a Timer Trigger - as opposed to being triggered with HTTP requests.

Second of all, previously we only had to deal with our own resources (the final step, where the gift is uploaded to a common container did not really cause any issues). However, in this part we will be facing something quite common in cloud scenarios - concurrency. But first things first

## Baseline

In your own resource group you should already have a working Azure Functions instance - the one with the HTTP triggered function. Aside from that, you should also have an Azure Storage Account - that is where (among other uses) the static website files from the first module are located.

Your resource group should look something like this:

![Initial state of resource group](../screenshots/resources_initial.png?raw=true "Initial state of resource group")

Your Azure Functions instance:

![Initial state of Azure Functions](../screenshots/functions_initial.png?raw=true "Initial state of Azure Functions")

Your Azure Storage Account:

![Initial state of Azure Storage](../screenshots/storage_initial.png?raw=true "Initial state of Azure Storage")

Once we have that, we can create all of the remaining pieces needed in this module.

In Storage Account Create a container and name it "stocking":

![New blob container creation](../screenshots/storage_new_container.png?raw=true "New blob container creation")

In Azure Functions create a new Timer triggered functions. And for now let's set the timer to be the midnight between Christmas Eve and Boxing Day, which can be defined by `0 0 0 25 12 *` in the Schedule value:

![New function creation](../screenshots/functions_new_timer_01.png?raw=true "New function creation")
![Timer triggered function](../screenshots/functions_new_timer_02.png?raw=true "Timer triggered function")
![Choose name and schedule](../screenshots/functions_new_timer_03.png?raw=true "Choose name and schedule")

## "Just make it work"

We will start with the simplest approach, and we will refine it later on. In summary the logic flow of the function would be

1. Connect to the "common blob container" - let's call it the Xmas tree
2. Connect to the "private blob container" - this one we will call the stocking
3. List all of the blobs/gifts in it
4. Choose a random gift from the list
5. "Move" it to the stocking

A rough scaffolding of this approach (but without any actual working code) can be found [here](Secret.Santa.Functions/ChooseRandomGift_0/). Feel free to copy it over to your function app and modify/fill out all the missing parts.

### Getting the nugets

Similarly to Module 3, we will be connecting to Azure Storage blob containers. There, we used a "built in" `#r "Microsoft.WindowsAzure.Storage"` reference, but in this module we want to show a very powerful functionality of Azure Functions - the ability to reference any public nuget package. For this we first need to tell the Azure Functions runtime that we will use external nuget libraries, and for this we need to create a `function.proj` file in the function's folder:

![Expand the View Files](../screenshots/functions_files_01.png?raw=true "Expand the View Files")
![Add new file](../screenshots/functions_files_02.png?raw=true "Add new file")
![Name it function.proj](../screenshots/functions_files_03.png?raw=true "Name it function.proj")

We will be using [Microsoft.Azure.Storage.Blob](https://www.nuget.org/packages/Microsoft.Azure.Storage.Blob) and [Microsoft.Azure.Storage.Common](https://www.nuget.org/packages/Microsoft.Azure.Storage.Common) nuget packages.

The exact syntax of the `function.proj` file can be found in the [official documentation](https://docs.microsoft.com/en-us/azure/azure-functions/functions-reference-csharp#using-nuget-packages)

In the end we should have something like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>netstandard2.0</TargetFramework>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="Microsoft.Azure.Storage.Blob" Version="11.1.0" />
        <PackageReference Include="Microsoft.Azure.Storage.Common" Version="11.1.0" />
    </ItemGroup>
</Project>
```

### Connecting to the blob containers

In previous module we have used a simplified approach to accessing the blobs through output bindings. For this module, we will need a bit more involved logic, as we not only need to get access to single blobs, but to the whole list of blobs in a container

Now we can finally start writing our code. For reference feel free to look at the [library documentation](https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.storage.blob?view=azure-dotnet-legacy) and a [.Net quickstart for blob storage](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet-legacy). First of all we will need to create an object that will represent our storage account. We also need to authorize the connection to that storage account and for this we will use a connection string. To get the connection string for your storage account, go to Settings -> Access Keys for that account and click on the "copy" icon to the right of the first connection string:

![Storage account connection string](../screenshots/storage_connectionstring.png?raw=true "Storage account connection string")

Since you don't have direct access to the Xmas tree storage account, you have no way of obtaining any access, but don't worry, everything required will be provided to you during the workshops.

For Xmas tree we will use something called the SAS token (SAS stands for [Shared Access Signature](https://docs.microsoft.com/en-us/azure/storage/common/storage-sas-overview)) - it is a way of sharing a limited access to parts of your storage. For the purpose of this workshops you will need access only to one container (not the whole storage account), and within that container, only listing, reading and deleting blobs is allowed. Accessing resources through SAS is through a Uri to the resource - for us it will be something like `https://thenameofthestorageaccount.blob.core.windows.net/xmastree`, and the SAS token in the following form: `?sp=rwdl&st=2019-12-11T14:50:59Z&se=2019-12-20T14:50:59Z&sv=2019-02-02&sr=c&sig=Zk5sDiwMz2X9vw3Mi4TGvWBCGu6thCyb%3D`, both will be provided to you.

Now that we have our stocking connection string, we can create the object that represent our storage account:

```cs
var stockingStorageAccount = CloudStorageAccount.Parse(stockingStorageConnectionString);
```

Once we have that, we can specify that we will be dealing with blobs specifically (since Azure Storage has option to deal with files, tables and queues as well, this distinction is required). For this we use

```cs
var stockingCloudBlobClient = stockingStorageAccount.CreateCloudBlobClient();
```

And finally, since the Storage Account can (and most often will) contain several containers, we need to specify, which of them we will be dealing with. We specify them by name:

```cs
var stockingCloudBlobContainer = stockingCloudBlobClient.GetContainerReference("stocking");
```

For the Xmas tree we were given access only to the container, so we can create it directly (without going through the CloudStorageAccount and CloudBlobClient objects, as we can't really access that). To create a `CloudBlobContainer` object, we just pass the `Uri` including the SAS token, like shown below:

```cs
var xmasTreeCloudBlobContainer = new CloudBlobContainer(new Uri(xmasTreeContainerUrl + xmasTreeSASToken), null);
```

### Listing blobs in container

In order to "randomly choose" a gift, we first need to know what is available - we need to get the whole list of blobs in the xmastree container. Let's check the (documentation)[https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.storage.blob.cloudblobcontainer?view=azure-dotnet-legacy] if there is anything that we could use. Sure enough there are a couple of methods that have a "List" in their name.

Quick note - we will be using the simplest method `ListBlobs`, but in production scenarios, the number of blobs stored in containers can be absolutely huge. All requests between cloud services and storage accounts go through the networks and this adds latency. That is why blob listing is by default paged and each page has at most 5000 elements. So in order to list all blobs, several requests would need to be made, and since all of them can take some time, it is best to use async. That is why methods like `ListBlobsSegmentedAsync` should generally be used. Fortunately for us, we won't be dealing with such huge amounts of data during these workshops, so we can just write:

```cs
var giftList = xmasTreeCloudBlobContainer.ListBlobs();
```

Looking at the documentation you might notice, that the `ListBlobs` returns an `IEnumerable` of `IListBlobItem`s, but all of the operations on blobs deal with objects of type `CloudBlockBlob`. Fortunately we can just cast to the type that we need:

```cs
var giftList = xmasTreeCloudBlobContainer.ListBlobs().Select(x => x as CloudBlockBlob);
```

### Getting a random element

Fortunately for us, .NET framework contains a powerful set of classes for dealing practically with anything we can think of, including "randomness". In addition, we can easily get a random integer in range `[0..n-1]`, which can then serve as an index for accessing elements in the gift list (make sure to turn it into a list first). What we get is similar to this:

```cs
var rnd = new Random();
int randomIndex = rnd.Next(giftList.Count);
var randomGift = giftList[randomIndex];
```

Quick note - it is generally inadvisable to create a new instance of `Random` every time, it is much better to create one common instance and re-use it. For this we can create and initialize a `static Random rnd` random generator outside the function body.

### Copy one blob to another

Just as before in Module 3, we will be uploading data to a blob, but now we don't get the blob object through the binding, but rather we need to create a reference to it ourselves. Since we already have our container, that will hold that blob, we can just write the following:

```cs
var stockingGift = stockingCloudBlobContainer.GetBlockBlobReference(randomGift.Name);
```

The easiest way to copy the contents of one blob to another is by getting it first to memory, and then uploading it to the target blob. We will use `MemoryStream` class for this. Caution - once we download data to stream, we need to "Seek" the stream back to the start before uploading it. And since `MemoryStream` is `IDisposable`, we can wrap everything in `using` statement for tidiness:

```cs
using(var memoryStream = new MemoryStream())
{
    randomGift.DownloadToStream(memoryStream);
    memoryStream.Seek(0, SeekOrigin.Begin);
    stockingGift.UploadFromStream(memoryStream);
}
```

### Move == Copy + Delete

We want to move the file, and so we need to delete the original gift after the upload was complete:

```cs
randomGift.DeleteIfExists();
```

### Run/compile/test

That pretty much should do it. Feel free to sprinkle the `log.LogInformation()` around your code to have a better understanding what`s what. In this stage you should get something like [the code shown in this directory](Secret.Santa.Functions/ChooseRandomGift_1/). Note - The ChooseRandomGift_1 directory contains a fully working code, but don't just copy it over, unless you are absolutely stuck.

## More async!

Even though the code may seem to run quickly, we are using quite a few blocking operations, mostly the ones connected to accessing the blobs, as they are network requests. We can easily fix that, because most of the methods have `async` equivalents. But first of all, we need to specify that the function will be running asynchronously, so we need to change the signature to:

```cs
public async static Task Run(TimerInfo myTimer, ILogger log)
```

Async functions have `async` keyword in front, and must return either a `Task` (for `void` returning equivalents) or `Task<T>` (for methods returning objects of type `T`). And now we can replace the methods into their asynchronous equivalents (remember to use the `await` keyword):

Downloading blob:
```cs
await randomGift.DownloadToStreamAsync(memoryStream);
```

Uploading blob:
```cs
await stockingGift.UploadFromStreamAsync(memoryStream);
```

Deleting blob:
```cs
await randomGift.DeleteIfExistsAsync();
```

Note - `ListBlobs` does not have a direct async equivalent, which is a shame.

After changing the code to be more asynchronous you should get something like [the code shown in this directory](Secret.Santa.Functions/ChooseRandomGift_2/).

## "But I wanted this gift!"

For now everything worked fine, as each of you were doing the tests independently. But imagine what would happen if all of you were to rush to the xmas tree - there would be conflicts - it is almost impossible that everyone would randomly choose a different gift than all others, some gifts would be chosen by several people. Let's all set the trigger time to the exact same minute and second (let's say the next minute divisible by 5). For that open the "Integrate" section of your Azure Function:

![Changing the schedule of an Azure Function](../screenshots/functions_schedule.png?raw=true "Changing the schedule of an Azure Function")

Let's now see, how it all turns out. Choosing randomly is not deterministic, so some of you might receive errors, some not, some of you may get the same gifts as others, some gifts will not be taken at all, there might be conflicts during deleting the blobs from xmastree. Let's fix that.

## Leasing

Fortunately for us, blobs in Azure Storage have just the right feature that will solve this problem - [blob leasing](https://docs.microsoft.com/en-us/rest/api/storageservices/lease-blob). Leasing is a mechanism that allows us to claim a blob just for us for some time - up to 60 seconds (it is possible to do an infinite lease, but we don't recommend it, because if the code crashes before we released the lease, it is complicated to "reset" the state of the blob so that is is accessible once again), and during that time, only the code that acquired the lease can do operations with the blob.

### First try

The first approach is to just try leasing the gift that we have randomly selected, with a `AcquireLease` method:

```cs
var randomGift = giftList[randomIndex];
string leaseID = null;
TimeSpan leaseTime = TimeSpan.FromSeconds(60);
leaseID = randomGift.AcquireLease(leaseTime, null);
```

Calling the `AcquireLease` throws an exception in case the blob was already leased. So now, if we use that code, there will be no conflicts, as once the gift is leased, it will be accessed only by one function, all others will crash. Try this out (set the same trigger time, like before).

### Improvement - if someone leased, choose another

This solution might "work" in a sense that we don't get duplicates, but the functions would need to be re-run for all of the unlucky cases, which is not ideal. Instead of crashing, we can keep trying to select another random gift, and see if that was successful. To avoid crashing the function on an unsuccessful, we wrap the `AcquireLease` in a `try catch` block:


```cs
CloudBlockBlob leasedGift = null;
string leaseID = null;
TimeSpan leaseTime = TimeSpan.FromSeconds(60);

var possibleGift = ...?;
try
{
    leaseID = possibleGift.AcquireLease(leaseTime, null);
    log.LogInformation($"Leasing successful {possibleGift.Name}");
    leasedGift = possibleGift;
}
catch(Exception)
{
    log.LogInformation($"The gift {possibleGift.Name} was already leased");
}
```

We don't really know how many times we will need to "try another random gift", but in the worst case scenario we will need to go through the whole list. And, in addition, it is pointless to try checking the same gift several times, we assume that if someone leased a blob, it will not be available to us any more.

With that in mind the best approach is to first randomize the whole list, and then iterate over all elements and the first one that is available will be ours to take.

To randomize a list we can sort it by a random number, like so:
```cs
var randomizedGifts = giftList.OrderBy(g => rnd.Next()).ToList();
```

Then, we loop through the whole randomized list:
```cs
foreach(var possibleGift in randomizedGifts)
{
    //our gift leasing logic here
}
```

And inside, we `try` to lease each gift one by one, and once we are successful, we can `break` out of the loop - we just need one gift.

```cs
CloudBlockBlob leasedGift = null;
string leaseID = null;
TimeSpan leaseTime = TimeSpan.FromSeconds(60);
foreach(var possibleGift in randomizedGifts)
{
    try
    {
        leaseID = possibleGift.AcquireLease(leaseTime, null);
        log.LogInformation($"Leasing successful {possibleGift.Name}")
        leasedGift = possibleGift;
        break;
    }
    catch(Exception)
    {
        log.LogInformation($"The gift {possibleGift.Name} was already leased, trying next");
    }
}
```

And if we are very very very unlucky (e.g. when more people want to take gifts from under the tree than was there in the first place) we need to check if in the end, we managed to lease any gift:

```cs
if(leasedGift == null){
    log.LogError($"All gifts were already leased, exiting");
    return;
}
```

### I have a lease, now what

Having the lease on a blob also means a bit of additional work - for example we can no longer "just delete" the blob like we did before, we need to say "I want to delete this blob, I know that it was leased by me, and here is the proof that I leased it and that I'm allowed to do anything with it".
The `DeleteIfExistsAsync` method has an overload that allows us to do it. The lease information needs to be wrapped into an `AccessCondition` object first and passed as one of the arguments of the delete method. This method overload takes some other arguments that we need to provide as well, but they are not important for us, so we will just provide the default values. So in the end we will have the following pieces of code:

```cs
AccessCondition acc = null;
///...
acc = new AccessCondition();
acc.LeaseId = leaseID;
//...
await leasedGift.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, acc, null, null);
```

After changing the code to use leasing and choosing the gift one by one iterating over a randomized list we should get something like [the code shown in this directory](Secret.Santa.Functions/ChooseRandomGift_3/).

## Copy blobs directly (bonus)

You may have noticed, that for copying the blob from one container to another we are using `MemoryStream`. What this means is that our Azure Functions instance needs to download and upload the whole contents of the blob. This is not a problem for our use-case, as our files would not exceed a couple of megabytes. However, if the file sizes or the amounts of processed files would get bigger, this approach would be inefficient and costly.

The easiest and most straightforward way would be to use the following:

```cs
await destBlob.StartCopyAsync(sourceBlob);
```

Unfortunately, this works only if we are copying blobs within the same storage account - in that situation no network data transfer would happen, the blob storage would just update references internally. Since our use-case involves copying blobs _between_ storage accounts, we need to use a bit different approach. One of the overloads of the copy method takes a `Uri` object - which we can create by using the `AbsoluteUri` of the leased gift combined with the SAS token of the xmastree container. This way we get the following:

```cs
var leasedGiftUrl = leasedGift.Uri.AbsoluteUri + xmasTreeSASToken;
await stockingGift.StartCopyAsync(new Uri(leasedGiftUrl));
```

This copy operation is run in the background, so our method may finish even before the copying was complete. If we want to continue once the blob was copied, we need to keep checking the status of the copy operation, like so:

```cs
while (stockingGift.CopyState.Status == CopyStatus.Pending)
{
    await Task.Delay(200);
    await stockingGift.FetchAttributesAsync();
}
```

A version of the code with direct copying can be found[here](Secret.Santa.Functions/ChooseRandomGift_4/).

## Extend the lease (bonus)

When acquiring the lease we have silently assumed that 60 seconds is going to be enough. And for our purposes this is true, as we are dealing with small files and we are just copying them. But what if we needed to do a bit more and hold the lease longer, and release it after some period that we can't specify or plan upfront.

For that we can use the `RenewLease` method of the blob. But this needs to run in the _background_ so that our regular logic flow does not have to be aware of the renewal. For running tasks in another thread we can use the `Task.Run` method, for which we provide the logic that needs to be executed. Inside we will wait some time just before the original lease expiration (in this example we'll wait 50 seconds), and then renew the lease, and loop.

One thing that you may immediately notice, is that the outside of the parallel code needs a way to "stop" the task from running - when we are done with the blob we no longer need to renew the lease. For that we can use `CancellationTokenSource` objects - we pass tokens created from it to the `Task.Run` method as one of the parameters, and once we are done, we call the `tokenSource.Cancel`. And once that happens, inside the Task we can check the status of the token by calling `token.ThrowIfCancellationRequested` method.

Finally, to avoid renewing the lease infinitely in case we forgot to cancel the token or if the logic crashed before we run the `Cancel` method. In that case instead of doing an infinite loop in the Task, let's just create a for loop that will eventually finish after some time (longer than the anticipated maximum time).

What we will have, will be similar to this:

```cs
try
{
    //once we have a leased gift, we can create a Task to renew the lease
    CancellationTokenSource tokenSource = new CancellationTokenSource();
    CancellationToken token = tokenSource.Token;
    Task.Run(() =>
    {
        try
        {
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(50));
                token.ThrowIfCancellationRequested();
                log.LogInformation("Renewing lease");
                leasedGift.RenewLease(acc);
            }
        }
        catch(OperationCanceledException)
        {
            log.LogInformation("Lease renewal canceled");
        }
    }, token);

    //do operations on the leased blob

}
finally
{
    tokenSource.Cancel();
}


```
