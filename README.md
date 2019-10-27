# Device Offline Detection with Durable Entities

This is the companion repository for this blogpost:

//todo: update
http://case.schollaart.net/2019/07/01/device-offline-detection-with-durable-entities.html

## Run it yourself

- Fork the repository
- Select or create an Azure DevOps project
- Make sure you have a Service Connection to Azure, [read here how to create one](https://docs.microsoft.com/en-us/azure/devops/pipelines/library/connect-to-azure?view=azure-devops)
- Update the 'variables' in the [azure-pipelines.yaml](azure-pipelines.yaml) files
	- azureSubscription: The name of the Service Connection
	- functionAppName: the name of your test project, lowercase no special characters, unique
	- resourceGroupName: the resource group to deploy to 
- Run the pipeline, read how here: ['Get your first run'](https://docs.microsoft.com/en-us/azure/devops/pipelines/create-first-pipeline?view=azure-devops&tabs=tfs-2018-2#get-your-first-run)
- go to http://{{functionAppName}}