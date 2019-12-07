# Azure Functions continued

If you are reading this not during workshops, but rather in your own time and prefer more detailed instructions that will give you a deeper understanding, head over to the [detailed version](Detailed/README.md).

## Introduction

<details>
<summary>
    Click to expand/collapse
</summary>

In this module we will create another Azure Function, which will randomly select one of the gifts in the common storage - we will call this the "xmas tree" and that gift will be moved to our own storage - we will call it the "stocking"

</details>

## Azure resources setup

<details>
<summary>
    Click to expand/collapse
</summary>

First, we need to make sure that we have a place where we can put out gifts - a blob storage container called "stocking". For this, in Azure Portal navigate to your Storage Account, under Blob Service click Containers, add a new Container and name it "stocking":

![New blob container creation](screenshots/storage_new_container.png?raw=true "New blob container creation")

Secondly, we need to make an Azure Function, and this time, instead of HttpTrigger, we will use a Timer Trigger. Open your Azure Function App and follow those steps:

![New function creation](screenshots/functions_new_timer_01.png?raw=true "New function creation")
![Timer triggered function](screenshots/functions_new_timer_02.png?raw=true "Timer triggered function")
![Choose name and schedule](screenshots/functions_new_timer_03.png?raw=true "Choose name and schedule")

</details>

## Set up Nuget packages

<details>
<summary>
    Click to expand/collapse
</summary>

Azure Functions allow you to use external libraries from public Nuget repository - thats around 180 thousand packages that can help you solve your problems. In order to use them, we need to add a `function.proj` file to our newly created function's folder. To do so, follow these steps:

![Expand the View Files](screenshots/functions_files_01.png?raw=true "Expand the View Files")
![Add new file](screenshots/functions_files_02.png?raw=true "Add new file")
![Name it function.proj](screenshots/functions_files_03.png?raw=true "Name it function.proj")

Inside the `function.proj` file paste in the following:

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

## Scaffolding

<details>
<summary>
    Click to expand/collapse
</summary>

Before we start coding, we need to come up with a rough plan on what our function needs to do. When we put down the information textually it will go something like this:

1. Connect to the "common blob container" - let's call it the xmastree
2. Connect to the "private blob container" - this one we will call the stocking
3. List all of the blobs/gifts in xmastree
4. Choose a random gift from the list
5. "Move" it to the stocking

We have created a scaffolding code below for this function with marked places where each piece of code should go. This code would not work yet, but we will work on this. Copy the snippet below to the `run.csx` file in your function:

```cs
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

## Connection strings

<details>
<summary>
    Click to expand/collapse
</summary>

The first part of the function body scaffolding contains a reference to connection strings. We need two of those - the connection string for xmastree will be shared with you during the workshops. Paste that connection string in the quotes in following place in the scaffolding:

```cs
// The xmastree is the "common storage" of gifts
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

## Connecting to storage accounts

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
    var xmasTreeCloudBlobContainer = xmasTreeCloudBlobClient.GetContainerReference("xmastree");
    var stockingCloudBlobContainer = stockingCloudBlobClient.GetContainerReference("stocking");
```

</details>

## List all the gifts 

<details>
<summary>
    Click to expand/collapse
</summary>

Now that we have objects representing the blob containers, we can do operations on them. First of all we need to get all of the available gifts in the xmastree container. We generally use `ListBlobs` method, but in addition we need to do some small operations so that the blobs are usable to us - we need to cast the returned objects to specific type and we need to have results in a list. In the end, what we get for step 3. is the following (plug this code into your scaffolding):

```cs
    // 3. List the blobs in the container.
    var giftList = xmasTreeCloudBlobContainer.ListBlobs().Select(x => x as CloudBlockBlob).ToList();
```

</details>

## Get a random gift

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

## Move the gift into your storage

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
    }

    // Once the copying was finished, delete the gift
    randomGift.DeleteIfExists();
```

</details>