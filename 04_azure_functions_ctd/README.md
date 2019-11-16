# Azure Functions continued

## Introduction
In the previous module we have used the Azure Functions in order to send an image to a common blob storage container. That action was triggered from a web page. In the current module we will continue our journey with Azure Functions, but we will look at them from a bit different angle.

First of all, we will be working on Functions that are executed on a schedule with a Timer Trigger - as opposed to being triggered with HTTP requests.

Second of all, previously we only had to deal with our own resources (the final step, where the gift is uploaded to a common container did not really cause any issues). However, in this part we will be facing something quite common in cloud scenarios - concurrency. But first things first

## Baseline

In your own resource group you should already have a working Azure Functions instance - the one with the HTTP triggered function. Aside from that, you should also have an Azure Storage Account - that is where (among other uses) the static website files from the first module are located.

Your resource group should look something like this:

![Initial state of resource group](screenshots/resources_initial.png?raw=true "Initial state of resource group")

Your Azure Functions instance:

![Initial state of Azure Functions](screenshots/functions_initial.png?raw=true "Initial state of Azure Functions")

Your Azure Storage Account:

![Initial state of Azure Storage](screenshots/storage_initial.png?raw=true "Initial state of Azure Storage")

Once we have that, we can create all of the remaining pieces needed in this module.

In Storage Account Create a container and name it "stocking":

![New blob container creation](screenshots/storage_new_container.png?raw=true "New blob container creation")

In Azure Functions create a new Timer triggered functions. And for now let's set the timer to be the midnight between Christmas Eve and Boxing Day, which can be defined by `0 0 0 25 12 *` in the Schedule value:

![New function creation](screenshots/functions_new_timer_01.png?raw=true "New function creation")
![Timer triggered function](screenshots/functions_new_timer_02.png?raw=true "Timer triggered function")
![Choose name and schedule](screenshots/functions_new_timer_03.png?raw=true "Choose name and schedule")

## "Just make it work"

We will start with the simplest approach, and we will refine it later on. In summary the logic flow of the function would be

1. Connect to the "common blob container" - let's call it the Xmas tree
2. Connect to the "private blob container" - this one we will call the stocking
3. List all of the blobs/gifts in it
4. Choose a random gift from the list
5. "Move" it to the stocking

A rough scaffolding of this approach (but without any actual working code) can be found [here](Secret.Santa.Functions/ChooseRandomGift_0/)

### Getting the nugets

If you did this in module 3, then feel free to skip to the next subsection.

Similarly to Module 3, we will be connecting to Azure Storage blob containers. For this we first need to tell the Azure Functions runtime that we will use external nuget libraries, and for this we need to create a `function.proj` file in the function's folder:

![Expand the View Files](screenshots/functions_files_01.png?raw=true "Expand the View Files")
![Add new file](screenshots/functions_files_02.png?raw=true "Add new file")
![Name it functions.proj](screenshots/functions_files_03.png?raw=true "Name it functions.proj")

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

If you did this in module 3, then feel free to skip to the next subsection.

Now we can finally start writing our code. For reference feel free to look at the [library documentation](https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.storage.blob?view=azure-dotnet-legacy) and a (.Net quickstart for blob storage)[https://docs.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet-legacy]. First of all we will need to create an object that will represent our storage account. We also need to authorize the connection to that storage account and for this we will use a connection string. To get the connection string for your storage account, go to Settings -> Access Keys for that account and click on the "copy" icon to the right of the first connection string:

![Storage account connection string](screenshots/storage_connectionstring.png?raw=true "Storage account connection string")

Since you don't have direct access to the Xmas tree storage account, you have no way of obtaining the connection string to it, but don't worry, it will be provided to you during the workshops.

Now that we have both connection strings, we can create the objects that represent our storage accounts:

```cs
var xmasTreeStorageAccount = CloudStorageAccount.Parse(xmasTreeStorageConnectionString);
var stockingStorageAccount = CloudStorageAccount.Parse(stockingStorageConnectionString);
```

Once we have that, we can specify that we will be dealing with blobs specifically (since Azure Storage has option to deal with files, tables and queues as well, this distinction is required). For this we

```cs
var xmasTreeCloudBlobClient = xmasTreeStorageAccount.CreateCloudBlobClient();
var stockingCloudBlobClient = stockingStorageAccount.CreateCloudBlobClient();
```

And finally, since the Storage Account can (and most often will) contain several containers, we need to specify, which of them we will be dealing with. We specify them by name. For the Xmas tree the name is `"xmastree"` and for the stocking it is `"stocking"`:

```cs
var xmasTreeCloudBlobContainer = xmasTreeCloudBlobClient.GetContainerReference("xmastree");
var stockingCloudBlobContainer = stockingCloudBlobClient.GetContainerReference("stocking");
```

### Listing blobs in container

In order to "randomly choose" a gift, we first need to know what is available - we need to get the whole list of blobs in the xmastree container. Let's check the (documentation)[https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.storage.blob.cloudblobcontainer?view=azure-dotnet-legacy] if there is anything that we could use. Sure enough there are a couple of methods that have a "List" in their name.

Quick note - in "production" scenarios, the number of blobs stored in containers can be absolutely huge. All requests between cloud services and storage accounts go through the networks and this adds latency. That is why blob listing is by default paged and each page has at most 5000 elements. So in order to list all blobs, several requests would need to be made, and since all of them can take some time, it is best to use async. That is why methods like `ListBlobsSegmentedAsync` should generally be used. Fortunately for us, we won't be dealing with such huge amounts of data during these workshops, so we can just use the simple `ListBlobs` method.

```cs
var giftList = xmasTreeCloudBlobContainer.ListBlobs();
```

Looking at the documentation you might notice, that the `ListBlobs` returns an `IEnumerable` of `IListBlobItem`s, but all of the operations on blobs deal with objects of type `CloudBlockBlob`. Fortunately we can just cast to the type that we need:

```cs
var giftList = xmasTreeCloudBlobContainer.ListBlobs().Select(x => x as CloudBlockBlob);
```

### Getting a random element

Fortunately for us, .NET framework contains a powerful set of classes for dealing practically with anything we can think of, including "randomness". In addition, we can easily get a random integer in range `[0..n-1]`, which can then serve as an index for accessing alements in the gift list (make sure to turn it into a list first). What we get is similar to this:

```cs
var rnd = new Random();
int randomIndex = rnd.Next(giftList.Count);
var randomGift = giftList[randomIndex];
```

Quick note - it is generally inadvisable to create a new instance of `Random` every time, it is much better to create one common instance and re-use it. For this we can create and initialize a `static Random rnd` random generator outside the function body.

### Copy one blob to another

Just as before in Module 3, we will be uploading data to a blob. For this we need a blob reference:

```cs
var stockingGift = stockingCloudBlobContainer.GetBlockBlobReference(randomGift.Name);
```

The easiest way to copy the contents of one blob to another is by getting it first to memory, and then uploading it to the target blob. We will use `MemoryStream` class for this. Caution - once we download data to stream, we need to "Seek" the stream back to the start before uploading it. And since `MemoryStream` is `IDisposable`, we can wrap everything in `using` statement for tidiness:

```cs
using(var memoryStream = new MemoryStream())
{
    // Download to memory first
    randomGift.DownloadToStream(memoryStream);
    // Reset the stream to upload from the start
    memoryStream.Seek(0, SeekOrigin.Begin);
    // Upload the stream
    stockingGift.UploadFromStream(memoryStream);
    // Set the content type to the original one (presumably "image/jpeg" or similar)
}
```

### Move == Copy + Delete

Since we want to move the file, we need to delete the original gift:

```cs
randomGift.DeleteIfExists();
```

### Run/compile/test

That pretty much should do it. Feel free to sprinkle the `log.LogInformation()` around your code to have a better understanding what`s what. In this stage you should get something like [this](Secret.Santa.Functions/ChooseRandomGift_1/)