# Azure Functions

## Introduction
In the previous module we have created a static website.
Now, we will be working on Functions that are triggered with HTTP requests.

## Baseline

In your own resource group you should have an Azure Storage Account - that is where (among other uses) the static website files from the first module are located.

## Create First Function

![](screenshots/Add-Function.PNG?raw=true "Add a Function")

Click **Add**. Then in the list of resources find and click "Function App".

![](screenshots/Add-Function2.PNG?raw=true "Add a Function")

Click **Create**.

![](screenshots/Add-Function3.PNG?raw=true "Add a Function")

Set a unique name for the function app, i.e. "search-for-a-gift". Choose **.NET Core** as running stack. Click **Review + create** and then click **Create**. You need to wait some time until the resource is created.

![](screenshots/rg1.PNG?raw=true "Function added")

You can see that other resources have also been created. Go to the newly created app service. Now you can see a little bit different view. Click on the blue plus sign next to **Functions**.

![](screenshots/fun1.PNG?raw=true "Function preview")

Now, choose **In-portal** development environment.

![](screenshots/fun2.PNG?raw=true "Function preview")

Great! Now choose **Webhook + API**.

![](screenshots/fun3.PNG?raw=true "Function preview")

Click **Create**. Your first function is ready to start! You can click **Run** to test it locally or **Get function URL** to use outside of Azure Portal.

## Cognitive Service

We need to create "Cognitive Services" to use Bing Search API to find images with gifts. To do so, add a new resource of type: "Cognitive Services". If you are not sure how to add a resource, look at the beginning of this tutorial to see how you have created Azure Function resource.

![](screenshots/cog1.PNG?raw=true "Cognitive Service")

Set a unique name, select a location (West Europe), you choose **S0** pricing tier this is enough for our needs.
The resource should be created quickly. When it is ready, you can go to the resource and you should see its key and endpoint.

![](screenshots/cog2.PNG?raw=true "Cognitive Service")

You will need them for upcoming development phase. :)

## Development Time!

Now the journey begins! In the code of your Azure Function, create an empty asynchronous function, let’s call it "ProcessSearch".

```cs
private static async Task<string> ProcessSearch(string searchTerm, ILogger log)
{
   // 1. Call the Bing API.
 
   // 2. Deserialize the JSON response from the Bing Image Search API.
 
   // 3. Save an image to the Azure Storage.
 
   return string.Empty;
}
```

Now copy the following code that searches for an image. Set subscription key and urlBase. You should copy them from Cognitive Services keys. Yes, these which you have already seen.

```cs
private static async Task<string> BingImageSearch(string searchTerm)
{
   // Replace the this string with your valid access key.
   const string subscriptionKey = "";
   const string uriBase = "";
 
   var uriQuery = uriBase + "bing/v7.0/images/search?q=" + Uri.EscapeDataString(searchTerm);
 
   WebRequest request = WebRequest.Create(uriQuery);
   request.Headers["Ocp-Apim-Subscription-Key"] = subscriptionKey;
   HttpWebResponse response = (HttpWebResponse) (await request.GetResponseAsync());
 
   return new StreamReader(response.GetResponseStream()).ReadToEnd();
}
```

Now copy the following code. When we save Blob to Azure Storage we have to specify its content type. The file extension is not enought for the storage.

```cs
public static string GetConentType(string fileName)
{
    string name = fileName.ToLower();
    string contentType = "image/jpeg";
    
    if (name.EndsWith("png"))
    {
        contentType = "image/png";
    }
    else if (name.EndsWith("gif"))
    {
        contentType = "image/gif";
    }
    else if (name.EndsWith("bmp"))
    {
        contentType = "image/bmp";
    }
    
    return contentType;
}
```

Now copy the following code that stores the image in the blob storage.

```cs
private static async Task SaveToStorage(string contentUrl, CloudBlockBlob outputBlob)
{
   outputBlob.Properties.ContentType = GetConentType(contentUrl);
 
   WebRequest request = WebRequest.Create(contentUrl);
   HttpWebResponse response = (HttpWebResponse) (await request.GetResponseAsync());
 
   using (Stream stream = response.GetResponseStream())
   {
       await outputBlob.UploadFromStreamAsync(stream);
   }
}
```

Does it compile? No! CloudBlockBlob is an unknown class type. We need to add one NuGet Package and include two necessary libraries. Add the new NuGet Package called: "*Microsoft.WindowsAzure.Storage*" at the top of the script. Use the following libraries:
"*Microsoft.WindowsAzure.Storage*", "*Microsoft.WindowsAzure.Storage.Blob*".
 
Check if everything compiles. Should be!!!

## Data Binding

We need to create a binding! Instead of adding connection string in the code we can make it easier. First, create an application setting. Go to **Configuration**.

![](screenshots/Binding1.PNG?raw=true "Binding")

Now, simply click **New application setting**.

![](screenshots/Binding2.PNG?raw=true "Binding")

The new dialog will appear. Set the name i.e. "STORAGE_BINDING" and paste the connection string to your local storage. At the final stage we will replace the local storage connection string with the shared one that will imitiate a christmas tree.
To get the connection string to the storage, I advise to open a new tab with Azure Portal. Go to your resource group and click the Storage account resource type.

![](screenshots/rg1.PNG?raw=true "Function added")

Then click **Access Keys** on the left. You should see a *key1*, *ConnectionString* etc., just like in the picture below. Copy the *ConnectionString*.

![](screenshots/Binding3.PNG?raw=true "Connection String To Storage")

Now paste the connection string to your app setting input field. Click **OK**. You should see the new setting in the list.

![](screenshots/Binding4.PNG?raw=true "Binding")

Remember to click **Save**. Good job! Now, go back to your Function App and click Integrate.

![](screenshots/Binding5.PNG?raw=true "Binding")

Add new output binding, then choose **Azure Blob Storage**:

![](screenshots/Binding6.PNG?raw=true "Binding")

Install necessary extension.

![](screenshots/Binding61.PNG?raw=true "Binding")

Once it has been installed, choose the created connection and click **Save**.

![](screenshots/Binding7.PNG?raw=true "Binding")

 You should see the binding like in the following picture:

![](screenshots/Binding8.PNG?raw=true "Binding")

If you click the link to **Advanced editor**, you should JSON configuration of bindings. Yes, go there and change only two things related to the *blob* binding section.
First, the path from **outcontainer/{rand-guid}** to **christmastree/{rand-guid}** and the direction from **out** to **inout**. Save changes.
Well the binding is ready, let’s put the last chunks of code.

## Let's finish it!

Add the parameter to the **Run** function: “CloudBlockBlob outputBlob”. Add that parameter to the **ProcessSearch** function too.

Fantastic! Now, go back to the ProcessSearch function. You have everything you need to fill in gaps in the code. You can copy all from the following snippet:

```cs
   // 1. Call the Bing API.
   var result = await BingImageSearch(searchTerm);
   // 2. Deserialize the JSON response from the Bing Image Search API.
   dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(result);
   var firstJsonObj = jsonObj["value"][0];
   string contentUrl = firstJsonObj["contentUrl"];
   // 3. Save an image to the Azure Storage.
   await SaveToStorage(contentUrl, outputBlob);

   return contentUrl;
```

Call the ProcessSearch function within the Run function.

```cs
var contentUrl = await ProcessSearch(name, log, outputBlob);
```

Awesome! You can add the function URL to the HTML page you have already created and be happy as it all works like a charm.
Copy the URL from the page where your function code is, click *</> Get Function URL* link.

![](screenshots/Url1.PNG?raw=true "Function URL")

Paste the URL to the index.html page and upload it to the storage.

![](screenshots/Url1.PNG?raw=true "Function URL")
 
OMG! I totally forgot! You have to enable CORS! You can read abour CORS [here](https://en.wikipedia.org/wiki/Cross-origin_resource_sharing).
Azure Function and Azure Storage are in different domains. We have to let the website talk to the function.
You should see the link to CORS setting like in the picture below.

![](screenshots/cors.PNG?raw=true "CORS")

After clicking **CORS**, paste the URL of your website there. **Save** changes.

## Testing

All in all, you can test if everything works. Think of unusual gift and send it. :)

![](screenshots/test1.PNG?raw=true "Testing")

In the storage you should see the **christmastree** container.

![](screenshots/test2.PNG?raw=true "Testing")

And a Blob inside it. Click the Blob name.

![](screenshots/test3.PNG?raw=true "Testing")

Click **Generate SAS** tab and then click **Generate SAS token and URL**. Copy Blob SAS URL and preview the gift in the browser. :)

![](screenshots/test4.PNG?raw=true "Testing")

# Function's Extras

## Introduction

Add a new function to get the uploaded image and post it to Azure Storage.

## Create new function in the existing Function app

1. Go to **Portal** then in Search input type **"Function"** and then select **Function App**.

![](screenshots/Create-Function1.PNG?raw=true "Create function")

2. Choose function app created before.

3. Create a new Function within the Function App.

![](screenshots/Create-Function2.PNG?raw=true "Create function")

4. Choose the **“HTTP Trigger”** for the Template.

![](screenshots/Create-Function3.PNG?raw=true "Create function")

5. Choose new name for the Function and select "Anonymous" for Authorization Level.

![](screenshots/Create-Function4.PNG?raw=true "Create function")

6. Modify function.json to enable Blob Output. Add in bidinngs section:
```
{
	"name": "blobContainer",
	"type": "blob",
	"path": "yourContainerName",
	"connection": "yourConnectionToStorageName",
	"direction": "out"
}
```

## Now, let's do actual development

Our function's skeleton looks like this:
```
//Imports and using

public static async Task<IActionResult> Run(HttpRequest req, CloudBlobContainer blobContainer, ILogger log)
{
    //Get file from HttpRequest

    //Save file in public blob

    //Return OK message
}

```

Let's walk through it together:

At first place, you can see all imports and usings which will be used in our code... but let's do this at the end ;)

Then there is a Function's method signature. Our Function will be asynchronous, accepts 3 arguments:
- http request which will contain image
- blob container to which we will upload the file
- logger

As a first step we will get file from the request, then save it and return information for the requestor that all went well.

### Get file from HTTP Request

It's pretty simple:
```
var file = req.Form.Files[0];
```

### Save file in blob container

It is also not much complicated... we need to make sure that blob container actually exists and create a blob block for our image. Then upload the image from the stream... and all is done!
```
await blobContainer.CreateIfNotExistsAsync();
var cloudBlockBlob = blobContainer.GetBlockBlobReference(blobName);
await cloudBlockBlob.UploadFromStreamAsync(file.OpenReadStream());
```

### Return response

```
return new OkObjectResult(blobName);
```

### Imports and usings

Last but not least:
```
#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"
#r "System.Net.Http"
#r "Microsoft.Azure.WebJobs"

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Net.Http;
using System.Net.Http.Headers;
```