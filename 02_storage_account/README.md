# Static website hosting in Azure Storage - Step by step

## 1. Create storage account

<details>
<summary>
Click to expand!
</summary>

![](screenshots/1portal_azure_home.png?raw=true "Azure portal home")

![](screenshots/p_a_sorage_accounts2.PNG?raw=true "Add new storage account")

### Hints:

<details>
<summary>
Click to expand!
</summary>

If there is no resource group you can choose, you must create one.
The name of storage account must be unique across all existing storage account names in Azure. It must be 3 to 24 characters long and can contain only lowercase letters and numbers.
Choose storage location carefully.

![](screenshots/pa_create_sa3.PNG?raw=true "Create storage account")

Fill all required data. Click “Review + create” button.
After successful validation press “Create” button.

![](screenshots/deploymentunderway4.PNG?raw=true "Deployment on the way")

![](screenshots/did-i-hear-someone-say-deployment.jpg?raw=true "Funny cat")

When your deployment is complete go to your new storage account.

</details>

</details>

## 2. Enable static website

<details>
<summary>
Click to expand!
</summary>

On the left side menu click "Static website"

![](screenshots/5gotostaticwebsite5.png?raw=true "Click static website")

![](screenshots/enablestaticwebsite6.PNG?raw=true "Enable static website")

Fill index document name and error path and save changes.

</details>

## 3. Azure Storage container

<details>
<summary>
Click to expand!
</summary>

After you save changes, an Azure Storage container has been created automatically to host your static website ($web)

![](screenshots/containercreated7.PNG?raw=true "Static website endpoints")

</details>

## 4. Go to container and choose $web

<details>
<summary>
Click to expand!
</summary>

![](screenshots/containerweb8.PNG?raw=true "Containers")

</details>

## 5. Upload files (index.html, 404.html, style.css, bg.jpg, extra.png)

<details>
<summary>
Click to expand!
</summary>

![](screenshots/uploadfiles9.PNG?raw=true "Upload files")

</details>

## 6. Go back to static website, copy primary endpoint, paste into browser and run

<details>
<summary>
Click to expand!
</summary>

![](screenshots/application.PNG?raw=true "Secret Santa Website")

</details>

## 7. Yay! It’s working!

<details>
<summary>
Click to expand!
</summary>

## ... and if it’s not:

![](screenshots/workingforme.jpg?raw=true "It's wroking fine for me")

</details>

# Extras

<details>
<summary>
Click to expand!
</summary>
	
Add ability to upload an image and send it to the function

## Steps
Follow the steps above to create and upload the files from `extras` subfolder

</details>
