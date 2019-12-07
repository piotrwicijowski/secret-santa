# Azure Functions continued

If you are reading this not during workshops, but rather in your own time and prefer more detailed instructions that will give you a deeper understanding, head over to the [detailed version](Detailed/README.md).

## Introduction

<details>
<summary>
    Click to expand
</summary>

In this module we will create another Azure Function, which will randomly select one of the gifts in the common storage - we will call this the "xmas tree" and that gift will be moved to our own storage - we will call it the "stocking"

</details>

## Azure resources setup

<details>
<summary>
    Click to expand
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
    Click to expand
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

## First version

<details>
<summary>
    Click to expand
</summary>

Before we start coding, we need to come up with a rough plan on what our function needs to do. When we put down the information textually it will go something like this:

1. Connect to the "common blob container" - let's call it the Xmas tree
2. Connect to the "private blob container" - this one we will call the stocking
3. List all of the blobs/gifts in it
4. Choose a random gift from the list
5. "Move" it to the stocking

</details>

