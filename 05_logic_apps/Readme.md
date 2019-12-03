# Logic apps

## Create blob trigger
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

## Debugging a Logic App

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