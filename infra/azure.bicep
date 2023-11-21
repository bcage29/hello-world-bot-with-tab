@maxLength(20)
@minLength(4)
param resourceBaseName string
param storageSku string

@description('Required when create Azure Bot service')
param botAadAppClientId string

@secure()
@description('Required by Bot Framework package in your bot project')
param botAadAppClientSecret string

param webAppSKU string

@maxLength(42)
param botDisplayName string

param serverfarmsName string = resourceBaseName
param webAppName string = resourceBaseName

param storageName string = resourceBaseName
param location string = resourceGroup().location
param aadAppClientId string
param aadAppTenantId string
param aadAppOauthAuthorityHost string
@secure()
param aadAppClientSecret string

// Azure Storage that hosts your static web site
resource storage 'Microsoft.Storage/storageAccounts@2021-06-01' = {
  kind: 'StorageV2'
  location: location
  name: storageName
  properties: {
    supportsHttpsTrafficOnly: true
  }
  sku: {
    name: storageSku
  }
}

// Compute resources for your Web App
resource serverfarm 'Microsoft.Web/serverfarms@2021-02-01' = {
  kind: 'app'
  location: location
  name: serverfarmsName
  sku: {
    name: webAppSKU
  }
}

// Web App that hosts your bot
resource webApp 'Microsoft.Web/sites@2021-02-01' = {
  kind: 'app'
  location: location
  name: webAppName
  properties: {
    serverFarmId: serverfarm.id
    httpsOnly: true
    siteConfig: {
      alwaysOn: true
      ftpsState: 'FtpsOnly'
    }
  }
}

resource webAppSettings 'Microsoft.Web/sites/config@2021-02-01' = {
  name: '${webAppName}/appsettings'
  properties: {
    WEBSITE_RUN_FROM_PACKAGE: '1'
    MicrosoftAppId: botAadAppClientId
    MicrosoftAppPassword: botAadAppClientSecret
    BOT_DOMAIN: webApp.properties.defaultHostName
    AAD_APP_CLIENT_ID: aadAppClientId
    AAD_APP_CLIENT_SECRET: aadAppClientSecret
    AAD_APP_TENANT_ID: aadAppTenantId
    AAD_APP_OAUTH_AUTHORITY_HOST: aadAppOauthAuthorityHost
    RUNNING_ON_AZURE: '1'
  }
}

// Register your web service as a bot with the Bot Framework
module azureBotRegistration './botRegistration/azurebot.bicep' = {
  name: 'Azure-Bot-registration'
  params: {
    resourceBaseName: resourceBaseName
    botAadAppClientId: botAadAppClientId
    botAppDomain: webApp.properties.defaultHostName
    botDisplayName: botDisplayName
  }
}

var siteDomain = replace(replace(storage.properties.primaryEndpoints.web, 'https://', ''), '/', '')

// The output will be persisted in .env.{envName}. Visit https://aka.ms/teamsfx-actions/arm-deploy for more details.
output TAB_AZURE_STORAGE_RESOURCE_ID string = storage.id // used in deploy stage
output TAB_DOMAIN string = siteDomain
output TAB_ENDPOINT string = 'https://${siteDomain}'
output BOT_AZURE_APP_SERVICE_RESOURCE_ID string = webApp.id
output BOT_DOMAIN string = webApp.properties.defaultHostName
