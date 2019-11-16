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

In Azure Functions create a new Timer triggered functions. And for now let's set the timer to be the midnight between Christmas Eve and Boxing Day:

![New blob container creation](screenshots/functions_new_timer_01.png?raw=true "New blob container creation")
![New blob container creation](screenshots/functions_new_timer_02.png?raw=true "New blob container creation")
![New blob container creation](screenshots/functions_new_timer_03.png?raw=true "New blob container creation")

## "Just make it work"

We will start with the simplest approach, and we will refine it later on. In summary the logic flow of the function would be

    1. Connect to the "common blob container" - let's call it the Xmas tree
    2. Connect to the "private blob container" - this one we will call the stocking
    3. List all of the blobs/gifts in it
    4. Choose a random gift from the list
    5. Copy it to the stocking
    6. Remove the gift from the Xmas tree

A rough scaffolding of this approach (but without any actual working code) can be found [here](Secret.Santa.Functions/ChooseRandomGift_0/)