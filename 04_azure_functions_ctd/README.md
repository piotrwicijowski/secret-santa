# Azure Functions continued

If you are reading this not during workshops, but rather in your own time and prefer more detailed instructions that will give you a deeper understanding, head over to the [detailed version](Detailed/README.md).

## :new_moon: Introduction

<details>
<summary>
    Click to expand/collapse
</summary>

In this module we will create another Azure Function, which will randomly select one of the gifts in the common storage - we will call this the "xmas tree" and that gift will be moved to our own storage - we will call it the "stocking".

</details>

## :new_moon: Azure resources setup

<details>
<summary>
    Click to expand/collapse
</summary>

First, we need to make sure that we have a place where we can put out gifts - a blob storage container called "stocking". For this, in Azure Portal navigate to your Storage Account, under Blob Service click Containers, add a new Container and name it "stocking":

![New blob container creation](screenshots/storage_new_container.png?raw=true "New blob container creation")

Secondly, we need to make an Azure Function, and this time, instead of HttpTrigger, we will use a Timer Trigger. Open your Azure Function App and create a new function called "DrawAGift" following these steps:

![New function creation](screenshots/functions_new_timer_01.png?raw=true "New function creation")
![Timer triggered function](screenshots/functions_new_timer_02.png?raw=true "Timer triggered function")
![Choose name and schedule](screenshots/functions_new_timer_03.png?raw=true "Choose name and schedule")

</details>

## :new_moon: Set up Nuget packages

<details>
<summary>
    Click to expand/collapse
</summary>

Azure Functions allow you to use external libraries from public Nuget repository - thats around 180 thousand packages that can help you solve your problems. In order to use them, we need to add a `function.proj` file to our newly created function's folder. To do so, follow these steps:

![Expand the View Files](screenshots/functions_files_01.png?raw=true "Expand the View Files")
![Add new file](screenshots/functions_files_02.png?raw=true "Add new file")
![Name it function.proj](screenshots/functions_files_03.png?raw=true "Name it function.proj")

Inside the `function.proj` file paste the following:

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

</details>

## :waxing_crescent_moon: Scaffolding

<details>
<summary>
    Click to expand/collapse
</summary>

Before we start coding, we need to come up with a rough plan on what our function needs to do. When we put down the information textually it will go something like this:

1. Connect to the "common blob container" - let's call it the christmastree
2. Connect to the "private blob container" - this one we will call the stocking
3. List all of the blobs/gifts in christmastree
4. Choose a random gift from the list
5. "Move" it to the stocking

We have created a scaffolding code below for this function with marked places where each piece of code should go. This code does not work yet, but we will fix that in a minute. Copy the snippet below to the `run.csx` file in your function:

```cs
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

    // 5. "Move" the gift to the stocking
    var ourGift = stockingCloudBlobContainer...
    ourGift....Upload/Copy/Move....randomGift....

}
```

</details>

## :waxing_crescent_moon: Connection strings

<details>
<summary>
    Click to expand/collapse
</summary>

The first part of the function body scaffolding contains a reference to connection strings. We need two of those - the connection string for christmastree will be shared with you during the workshops. Paste that connection string in the quotes in following place in the scaffolding:

```cs
// The christmastree is the "common storage" of gifts
const string xmastreeStorageConnectionString = "you'll get this during the workshops";
```

When it comes to the connection string for your own storage account, then you need to extract it yourself by following these steps:

- Go to your Storage Account
- Click on the Settings -> Access keys option
- You will see key1 and key2 sections, and under both there is a Connection string entry. You can choose either of these. On the very right of the string there is a copy button.

![Storage account connection string](screenshots/storage_connectionstring.png?raw=true "Storage account connection string")

Paste that connection string in the quotes in the following place in the scaffolding:

```cs
// The stocking is your personal storage for your gift
const string stockingStorageConnectionString = "you'll paste your own connection string here";
```

</details>

## :first_quarter_moon: Connecting to storage accounts

<details>
<summary>
    Click to expand/collapse
</summary>

Now, since we have the connection strings, we can connect to the storage accounts (steps 1. and 2.). For this we use `Parse` method of `CloudStorageAccount`, `CreateCloudBlobClient` for each account and `GetContainerReference` of each blob client. In the end, what we get for steps 1. and 2. is the following (plug this code into your scaffolding):

```cs
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
```

</details>

## :first_quarter_moon: List all the gifts 

<details>
<summary>
    Click to expand/collapse
</summary>

Now that we have objects representing the blob containers, we can do operations on them. First of all we need to get all of the available gifts in the christmastree container. We generally use `ListBlobs` method, but in addition we need to do some small operations so that the blobs are usable to us - we need to cast the returned objects to specific type and we need to have results in a list. In the end, what we get for step 3. is the following (plug this code into your scaffolding):

```cs
    // 3. List the blobs in the container.
    var giftList = xmasTreeCloudBlobContainer.ListBlobs().Select(x => x as CloudBlockBlob).ToList();
```

</details>

## :first_quarter_moon: Get a random gift

<details>
<summary>
    Click to expand/collapse
</summary>

Now that we have a list of all available gifts, we choose one at random. For this we will use built-in .Net class `Random`, and its method `Next` that returns a random number in range `[0..Count]`. In the end, what we get for step 4. is the following (plug this code into your scaffolding):

```cs
    // 4. Pick random gift
    // Get a random index in the range [0..Count-1] and get a gift from the list with that index
    var rnd = new Random();
    int randomIndex = rnd.Next(giftList.Count);
    var randomGift = giftList[randomIndex];
```

</details>

## :first_quarter_moon: Move the gift into your storage

<details>
<summary>
    Click to expand/collapse
</summary>

Finally, we need to move that blob to our container. To move a blob, we first need to copy it - for that we will use `MemoryStream` - we will download the blob to that stream first, and then upload the stream into target blob. Finally, once the blob is copied, we can delete the original blob from under the xmas tree. In the end, what we get for step 5. is the following (plug this code into your scaffolding):

```cs
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
        // Set the content type to the original one (presumably "image/jpeg" or similar)
        stockingGift.Properties.ContentType = randomGift.Properties.ContentType;
        stockingGift.SetProperties();
    }

    // Once the copying was finished, delete the gift
    randomGift.DeleteIfExists();
```

</details>

## :first_quarter_moon: Time to test!

<details>
<summary>
    Click to expand/collapse
</summary>

With all these pieces in place we are ready to run the function. Even though the function is set up to run on schedule, we can also run it with "Run" button at the top of the function's editing area.

When you run the function successfully, you can check the result by going to your storage account, opening the Storage Explorer, expanding the Blob Containers and looking into the stocking container:

![Gift in stocking container](screenshots/storage_first_gift.png?raw=true "Gift in stocking container")

You should now have a working basic version of the function. If at this point you had some issues along the way, or the function does not compile, you can use [this checkpoint of the code](Secret.Santa.Functions/ChooseRandomGift_1/run.csx) - just make sure to replace the connection strings at the beginning.

</details>

## :waxing_gibbous_moon: Conflict resolution

<details>
<summary>
    Click to expand/collapse
</summary>

For now everything was nice and orderly - since during tests everyone ran their function at different moments, there were no fights for gifts, no access conflicts. But as you may imagine, if this was ran at schedule and every function was executed in the same second, some gifts may be copied by many people, some gifts may be broken (if a gift was deleted while another one was in the process of copying it). Let's fix this.

Fortunately, Azure Blobs give us a nice mechanism for claiming a blob - [blob leasing](https://docs.microsoft.com/en-us/rest/api/storageservices/lease-blob). If one client leases a blob, all other clients trying to lease will throw an exception.

With that in mind, let's think for a second, how to re-think the code. Of course we need to get some gift, so if our leasing fails, we can't crash, but rather we need to keep trying until we get some gift. And if we are very unlucky, we will have to try many gifts, so we need to loop over them. And once we are successful with leasing a gift, we should not check and lease other remaining gifts.

</details>

## :waxing_gibbous_moon: Leasing

<details>
<summary>
    Click to expand/collapse
</summary>

So, with all that information about leasing, we need to replace the logic for our section 4. that is used to pick random gifts. Inside, we will randomize the list of gifts, iterate over all of them, try leasing the gifts one by one, and once we succeed, we can move further. Replace the part 4. of your function with the following snippet:

```cs
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
```

And one more thing - once the blob is leased, then in order to delete it we need to prove that we are the ones who leased it. Replace the last `DeleteIfExists` statement in your function with the following line:

```cs
    randomGift.DeleteIfExists(DeleteSnapshotsOption.IncludeSnapshots, acc, null, null);
```

</details>

## :full_moon: That's it

<details>
<summary>
    Click to expand/collapse
</summary>

You should now have a working, robust function that will be resistant to conflicts and will peacefully co-exist with others. If at this point you had some issues along the way, or the function does not compile, you can use [this checkpoint of the code](Secret.Santa.Functions/ChooseRandomGift_2/run.csx) - just make sure to replace the connection strings at the beginning.

And if you are hungry for even more details, explanations and in-depth info, feel free to check out the [detailed version](Detailed/README.md) of this Readme

</details>
