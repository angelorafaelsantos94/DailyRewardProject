@allowed([
  'dev'
  'staging'
  'prod'
])
param environment string = 'dev'

param location string = resourceGroup().location

param playFabTitleId string = 'TEST_TITLE'
param dailyRewardAmount string = '50'
param dailyRewardCurrencyId string = 'currency.soft'

var suffix = uniqueString(resourceGroup().id)

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: 'dailyreward${suffix}'
  location: location

  sku: {
    name: 'Standard_LRS'
  }

  kind: 'StorageV2'

  properties: {
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
  }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: 'dailyreward-func-${environment}'
  location: location
  kind: 'functionapp'

  properties: {
    siteConfig: {
      appSettings: [
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'PLAYFAB_TITLE_ID'
          value: playFabTitleId
        }
        {
          name: 'DAILY_REWARD_AMOUNT'
          value: dailyRewardAmount
        }
        {
          name: 'DAILY_REWARD_CURRENCY_ID'
          value: dailyRewardCurrencyId
        }
        {
          name: 'PLAYFAB_SECRET_KEY'
          value: '@Microsoft.KeyVault(SecretUri=https://placeholder-kv.vault.azure.net/secrets/playfab-secret)'
        }
      ]
    }
  }
}