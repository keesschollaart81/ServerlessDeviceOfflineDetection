# Device Offline Detection with Durable Entities

This is the companion repository for this blogpost:

http://case.schollaart.net/2019/10/31/device-offline-detection-with-durable-entities.html

## Run it yourself

You have 2 options: 

1. Create infrastructure with ARM Template and then 'Right Click Deploy' the Function App to it
2. Fork the repository, use the Azure Pipelines YAML to do all the work

### Deployment option 1 

1. Click the Button below to create the infrastructure in your Azure Subscription

   [![Deploy to Azure](https://azuredeploy.net/deploybutton.png)](https://azuredeploy.net/)
   
2. Clone this repository to your machine
3. Right Click Deploy via an IDE with Azure Functions Tooling installed
	a. VS Code with [read more](https://docs.microsoft.com/en-us/azure/azure-functions/functions-develop-vs-code#publish-to-azure) ([screenshot](images/step7.png))
	b. Visual Studio 2019 [read more](https://docs.microsoft.com/en-us/azure/azure-functions/functions-develop-vs#publish-to-azure) ([screenshot](images/step7b.png))
4. Continue reading '[Running it]('running-it')'

### Deployment option 2 (via Azure Pipelines)

- Fork the repository and clone it to your machine
- Go to (or create) an Azure DevOps project where you want the pipline to live
- Make sure you have a Service Connection to an Azure Subscription [read here how to create one](https://docs.microsoft.com/en-us/azure/devops/pipelines/library/connect-to-azure?view=azure-devops)
	- Remember the exact name of this Service Connection
- Create the pipeline in Azure DevOps based on [azure-pipelines.yaml](azure-pipelines.yaml). Read how here how to ['Get your first run'](https://docs.microsoft.com/en-us/azure/devops/pipelines/create-first-pipeline?view=azure-devops&tabs=tfs-2018-2#get-your-first-run)
	- Step by Step screenshots [1](images/step1.png), [2](images/step2.png), [3](images/step3.png), [4](images/step4.png), [5](images/step5.png)
	- Update the variables in the azure-pipelines.yaml, there will be a screen to do that during the Azure DevOps pipeline creation wizard (screenshot 5)
		- azureSubscription: The name of the Service Connection
		- functionAppName: the name of your test project, make it lowercase no special characters and it has to be globally unique
		- resourceGroupName: the resource group to deploy to 
	- Make sure the build and deploy were succesfull ([screenshot](images/step6.png))

### Running it

- Go to https://{functionAppName}.azurewebsites.net/api/dashboard, here you should see a Header called 'Devices'
- Update the ConnectionString to the Storage Queue in the TestDevice project
	- Go to the Azure Portal and go to the `{functionAppName}st` Storage Account resource (dont mistake it for the storage account ending op `dst`)
	- Click on 'Access Keys' and copy the Connection String
	- Open this cloned repository/solution on your machine with VS Code or Visual Studio and go to the TestDevice project
	- Edit the appsettings.json and update the `StorageConnectionString`
- Run the TestDevice console app [src/TestDevice] (`dotnet run`) and observe the status in the dashboard, for example, start with 200 devices and then change it to 100