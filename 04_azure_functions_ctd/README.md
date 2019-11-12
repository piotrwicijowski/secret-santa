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