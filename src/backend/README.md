# Epicom Middleware
​
## Building and Running the Function App
​
You can build the function app via Visual Studio or `func` command line tooling.

For function app tooling and guidance see [here](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=windows%2Ccsharp%2Cbash).

The file `local.settings.json` contains the minimal setup for running locally. You need to run the Azure Storage Emulator. 
- [Microsoft Azure Storage](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator)
- [Azurite](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-azurite?toc=/azure/storage/blobs/toc.json) (open source alternative).

Navigate to `\src\backend\FunctionApps\FL.Ebolapp.FunctionApps.Fetch`
- `func start --build`

The function specific configuration is missing. Create a user secrets json and set them up appropriatly. See `\docs\fetch_function_user_secrets_template.json` for a template.