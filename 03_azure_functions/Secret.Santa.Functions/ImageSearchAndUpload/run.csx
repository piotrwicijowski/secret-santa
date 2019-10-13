#r "Newtonsoft.Json"

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Microsoft.Azure.CognitiveServices.Search.ImageSearch;
using Microsoft.Azure.CognitiveServices.Search.ImageSearch.Models;

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
    log.LogInformation("C# HTTP trigger function processed a request.");

    var formdata = await req.ReadFormAsync();
    string query = formdata["query"];

    //IMPORTANT: replace this variable with your Cognitive Services subscription key
    string subscriptionKey = "ENTER YOUR KEY HERE";
    //stores the image results returned by Bing
    Images imageResults = null;

    //initialize the client
    var client = new ImageSearchClient(new ApiKeyServiceClientCredentials(subscriptionKey));

    return query != null
        ? (ActionResult)new OkObjectResult($"Hello, {query}")
        : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
}
