# EPICOM APP - Middleware

## About the App

This is the source code for the middleware of the EPICOM App, an app where you can check the current Corona infection rate at your location or in another area. It's kind of your "compass" for the pandemic. EPICOM warns you if you are approaching a risk area with a higher infection rate than that at your current location or if you were recently in a location that has meanwhile become a risk area. You can track your own movement profile of the last 14 days in the app and recognise when you have been in one place where for more than 15 minutes. Contacts with a risk of infection are so easy for you to reconstruct. EPICOM also has links to all official websites of state governments, municipalities and districts with codes of conduct and official instructions regarding Corona. In this way, you will receive the most important information you need for your self and to protect your environment.
For more information about the app please go to https://www.epicom.online

## Technical Overview

The middleware is developed in C# / .NET Core 3.1 runs under the FunctionApp SDK and is deployed as Azure Function App. 

The Middleware requests daily the Covid-19 case data from the RKI API and provides it for the apps.

## Requirements

* .NET Core = 3.1
* C# = 8.0
* dotnet cli = 3.1
* Azure Functions Core Tools >= 3.x

## Configurations

User Secret configuration is not added under souce control. Please see `"\docs\fetch_function_user_secrets_template.json` for a template and adjust accordingly.

## Ebolapp vs Epicom?

Why is the project called Ebolapp, but the App itself Epicom? Ebolapp is the name, the previous app was drafted with. It was concepted to help with the Ebola Pandemic in West Africa in 2014/15. As the current Version is limited to the Covid-19 Risk Areas in Germany, it was renamed to Epicom. To find out more about the history of the app, please go to https://www.epicom.online

## Contribution

Please check the `CONTRIBUTING.md` on how to colloborate on the project.

## Project Overview

This middleware consists of an Azure Function with two timer triggers and a Azure CDN with Azure Blob Storage as a source for the app data files.
    
- Daily: Triggers once a day (configured in Azure Function App Configuration section).
- Hourly: Triggers once every full hour (configured in Azure Function App Configuration section).
- The transformed data will be stored in the blob storage that is used by the CDN to make the data public for the apps.

## Project Local Setup
- see `src\backend\README.md` to find the setup details

## Project Maintenance
- CI/CD pipelines [here](https://dev.azure.com/Af-Freunde-Liberias/Freunde-Liberias-EBOLAPP-Backend/_build)
- deployed to `Freunde Liberias - EBOLAPP` subscription


## Azure Resource Deployment
Azure resources are managed via `pulumi` as `Infastructure as Code` find more information in the `README.md` under `deployment\pulumi`.
