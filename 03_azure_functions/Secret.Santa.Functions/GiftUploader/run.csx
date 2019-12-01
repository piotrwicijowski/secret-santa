//Imports and using

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

public static async Task<IActionResult> Run(HttpRequest req, out CloudBlobContainer blobContainer, ILogger log)
{
    //Get file from HttpRequest
    var file = req.Form.Files[0];

    var blobName = $"{file.FileName}";    

    //Save file in public blob
    await blobContainer.CreateIfNotExistsAsync();
    var cloudBlockBlob = blobContainer.GetBlockBlobReference(blobName);
    await cloudBlockBlob.UploadFromStreamAsync(file.OpenReadStream());

    //Return OK message
    return new OkObjectResult(blobName);
}
