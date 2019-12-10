# Logic apps

## Create blob trigger

<details>
<summary>
Click to expand!
</summary>

 * Open Azure portal (https://portal.azure.com)
 * Create a new LogicApp
 
 ![New logic app](screenshots/new-logic-app.png?raw=true)

* Fill out the details:
  - choose a relevant name for the Logic APp
  - use your own resource group name
  - click `Create`

![New logic app details](screenshots/new-logic-app-details.png?raw=true)

* When you see a popup `Deployment succeeded`, click `Go to resource` button
* Go to `Logic app designer` in the main menu of the Logic App

![New logic app blade](screenshots/logic-app-blade.png?raw=true)

* Scroll down and select `Blank Logic App` template

![](screenshots/templates.png?raw=true)

* Define a trigger
  - In the search, type `blob`
  - Select the `When a blob is added or modified` trigger

![](screenshots/blob-trigger.png?raw=true)

* Configure the trigger as following
  - Select your container with gifts in `Container` field
  - Set the `Number of blobs to return from trigger` to `1`
  - You may want to change the interval value to e.g. `15 seconds` to speed up the diagnostics process (optional)
  - If you cannot find your storage account or your container, click `Change connection` at the bottom of trigger configuration
![](screenshots/trigger-config.png?raw=true)

* Click `+ New step` and search for `SAS`
  - Note that `SAS` in Azure Storage Account stands for Shared Access Signature see (https://docs.microsoft.com/en-us/azure/storage/common/storage-sas-overview for details) 

![](screenshots/new-step.png?raw=true)
![](screenshots/find-sas.png?raw=true)

* Create the action and configure it as follows:
  - Click on the text field next to `Blob path`
  - A popup opens with a list of outputs from other triggers and actions
  - Select `List of Files Path`. This is a full path Note that the name might be misleading as we chose just a single file to be returned.
  - Keep the `Permissions` value as `Read`

![](screenshots/sas-blob-path.png?raw=true)

* Create yet another step: 
  - Click `+ New step`
  - Type `slack`
  - In the bottom actions panel select `Post message` action

  ![](screenshots/new-slack.png?raw=true)

  - Authorize slack to use your account
  - Pick the `gifts` channel
  - Click on the text field next to `Message Text` and select `Web Url` from the popup

  ![](screenshots/slack-config.png?raw=true)

 </details>
  
## Debugging a Logic App

<details>
<summary>
Click to expand!
</summary>

* Click on `Save` and `Run`

![](screenshots/waiting.png?raw=true)

* In a new browser window, open Azure Portal again
* Navigate to the storage account with your gifts
* Open `Storage explorer`

![](screenshots/storage-explorer.png?raw=true)

* Select the proper blob container and click `Upload`

![](screenshots/storage-explorer-upload.png?raw=true)

* Select an image file to be uploaded to the storage and click `Upload`
* Switch to the Logic App tab in your browser and wait until you see a result
  - Each successfully executed trigger or action is now marked with a green/red icon.

![](screenshots/app-run.png?raw=true)


* You may further dig into each element inputs/outputs by clicking on the header

![](screenshots/run-details.png?raw=true)

* You should also be able to see your gift on the slack channel:

![](screenshots/slack-result.png?raw=true)

* You may modify the `Slack` action parameters to format the message better (e.g. enable `Post As User`)

</details>

# Logic App Extras

<details>
<summary>
Click to expand!
</summary>

## Introduction

Use Cognitive Services to find the most related tags and post it as a hashtags in Logic App.

All steps for our extra part, we will do before sending image to the Slack.

## Get Cognitive services url and key

<details>
<summary>
Click to expand!
</summary>

Go to your Resource group and click on already created Cognitive Services. 

Then get url from **Overview** -> **Endpoint**.

![](screenshots/get-url.png?raw=true)

To get a key go to **Keys** and copy **Key 1**.

![](screenshots/get-key.png?raw=true)

</details>

## Add Computer Vision Tag block in existing Logic App

<details>
<summary>
Click to expand!
</summary>

We will start with adding new block in the Logic App. New step should be added before the step to publish gift to the social media.

Choose **Computer Vision API** action and select **Tag Image**:

![](screenshots/select-tag-image.png?raw=true)

Add information about your Cognitive Services:

![](screenshots/add-computer-vision.png?raw=true)

After filling the Cognitive Servies, select **Image Url** in **Image Source**. Also in **Add new parameter** input, select **Image URL**:

![](screenshots/select-image-url.png?raw=true)

then select **Web-url** from dynamic content:

![](screenshots/image-url-web-url.png?raw=true)

</details>

## Add tags to the string variable

<details>
<summary>
Click to expand!
</summary>

After all of that we need to add two steps to our Logic App:
- Initialize variable
- Select names from tag list
- Join names with # to create hashtags
- Append hashtags to variable created before

Add **Initialize variable** from Variables  as one below:

![](screenshots/initialize-variable.png?raw=true)

After this step, add new step called **Select** from Data Operations connector:

![](screenshots/add-select.png?raw=true)

To select the collection fromt which we will select values, in **From** input select "tags", and in **Map** input choose "Tag Name"

![](screenshots/select-tags.png?raw=true)

![](screenshots/select-tag-names.png?raw=true)

Now, it is a time to join all names to create hashtags. Add new step and search for **Join** from Data Operations connector:

![](screenshots/add-join.png?raw=true)

In the **From** input choose Select Output and in **Join with** put ` #`:

![](screenshots/join.png?raw=true)

Last part is to appennd hastags to Choose **Append to string variable** action from Variables connector. 

![](screenshots/append-to-string-variable.png?raw=true)

Then choose variable which you have initialize before. In the **Value** put Expression: `concat('#', body('Join'))` 

![](screenshots/append-to-string-variable-2.png?raw=true)

</details>

## Connect variable with "Post message" step

<details>
<summary>
Click to expand!
</summary>

Then connect this value in slack message you will send. In the **Message Text** the reference to your variable:

![](screenshots/post-message-with-hashtags.png?raw=true)

</details>

</details>
