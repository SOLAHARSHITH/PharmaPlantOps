targetScope = 'resourceGroup'

param location string = resourceGroup().location
param appName string = 'plantops'
param sqlAdminLogin string
param operatorUsername string = 'operator'
param supervisorUsername string = 'supervisor'
param deployerPrincipalId string
param deployerPrincipalType string = 'User'

@secure()
param sqlAdminPassword string

@secure()
param jwtSigningKey string

@secure()
param operatorPassword string

@secure()
param supervisorPassword string

var suffix = uniqueString(resourceGroup().id)
var apiHostName = '${appName}-api-${suffix}.azurewebsites.net'
var webHostName = '${appName}-web-${suffix}.azurewebsites.net'
var keyVaultSecretsUser = '4633458b-17de-408a-b874-0445c86b69e6'
var keyVaultSecretsOfficer = 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7'

resource logs 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${appName}-logs-${suffix}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${appName}-ai-${suffix}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logs.id
  }
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: '${appName}-sql-${suffix}'
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlFirewall 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: 'PlantOps'
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${appName}kv${suffix}'
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
  }
}

resource deployerSecretRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, deployerPrincipalId, keyVaultSecretsOfficer)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsOfficer)
    principalId: deployerPrincipalId
    principalType: deployerPrincipalType
  }
}

resource apiSecretRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, api.id, keyVaultSecretsUser)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUser)
    principalId: api.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource sqlConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'sql-connection-string'
  properties: {
    value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=PlantOps;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=True'
  }
  dependsOn: [
    deployerSecretRole
  ]
}

resource jwtSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'jwt-signing-key'
  properties: {
    value: jwtSigningKey
  }
  dependsOn: [
    deployerSecretRole
  ]
}

resource operatorPasswordSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'operator-password'
  properties: {
    value: operatorPassword
  }
  dependsOn: [
    deployerSecretRole
  ]
}

resource supervisorPasswordSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'supervisor-password'
  properties: {
    value: supervisorPassword
  }
  dependsOn: [
    deployerSecretRole
  ]
}

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${appName}-plan-${suffix}'
  location: location
  kind: 'linux'
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  properties: {
    reserved: true
  }
}

resource api 'Microsoft.Web/sites@2023-12-01' = {
  name: '${appName}-api-${suffix}'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'ConnectionStrings__PlantOps'
          value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=sql-connection-string)'
        }
        {
          name: 'Auth__JwtSigningKey'
          value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=jwt-signing-key)'
        }
        {
          name: 'Auth__OperatorPassword'
          value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=operator-password)'
        }
        {
          name: 'Auth__SupervisorPassword'
          value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=supervisor-password)'
        }
        {
          name: 'Auth__OperatorUsername'
          value: operatorUsername
        }
        {
          name: 'Auth__SupervisorUsername'
          value: supervisorUsername
        }
        {
          name: 'Auth__JwtIssuer'
          value: 'PlantOps'
        }
        {
          name: 'Auth__JwtAudience'
          value: 'PlantOps'
        }
        {
          name: 'Cors__Origin'
          value: 'https://${webHostName}'
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
      ]
    }
  }
  dependsOn: [
    sqlDatabase
    sqlFirewall
  ]
}

resource web 'Microsoft.Web/sites@2023-12-01' = {
  name: '${appName}-web-${suffix}'
  location: location
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'NODE|20-lts'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'NEXT_PUBLIC_API_URL'
          value: 'https://${apiHostName}'
        }
      ]
    }
  }
}

output apiHostName string = apiHostName
output webHostName string = webHostName
output sqlServerName string = sqlServer.name
output keyVaultName string = keyVault.name
output applicationInsightsName string = appInsights.name
