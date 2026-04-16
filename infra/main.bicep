// Azure Container Apps + Cosmos DB (serverless) deployment.
// All resources fit within Azure's free / low-cost tiers for a demo workload.
//
// Deploy:  az deployment group create \
//            --resource-group taskboard-rg \
//            --template-file infra/main.bicep \
//            --parameters ghcrToken=<PAT>
//
// Or use:  mise run infra:up

targetScope = 'resourceGroup'

// ── Parameters ────────────────────────────────────────────────────────────────

@description('Azure region — defaults to the resource group location')
param location string = resourceGroup().location

@description('Short name prefix used in all resource names')
param appName string = 'taskboard'

@description('Docker image tag to deploy (e.g. git SHA or "latest")')
param imageTag string = 'latest'

@description('GitHub Container Registry username (image owner)')
param ghcrUsername string = 'mac9sb'

@description('GHCR Personal Access Token with read:packages scope — stored as a Container Apps secret')
@secure()
param ghcrToken string

// ── Variables ─────────────────────────────────────────────────────────────────

// Cosmos DB account names must be globally unique
var suffix = uniqueString(resourceGroup().id)
var cosmosAccountName = '${appName}-${suffix}'
var imageBase = 'ghcr.io/${ghcrUsername}/taskboard'

// ── Cosmos DB (serverless — pay per request, ~$0 for low traffic) ─────────────

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: cosmosAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: { defaultConsistencyLevel: 'Session' }
    locations: [{ locationName: location, failoverPriority: 0, isZoneRedundant: false }]
    capabilities: [{ name: 'EnableServerless' }]
    disableLocalAuth: false
  }
}

resource cosmosDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: cosmosAccount
  name: 'taskboard'
  properties: {
    resource: { id: 'taskboard' }
    // No throughput block — inherited from serverless account
  }
}

// ── Log Analytics (required by Container Apps; 5 GB/month free) ──────────────

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${appName}-logs'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

// ── Container Apps Environment ────────────────────────────────────────────────

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${appName}-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// ── Backend Container App (internal ingress — not publicly reachable) ─────────

var cosmosConnectionString = cosmosAccount.listConnectionStrings().connectionStrings[0].connectionString

resource backend 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${appName}-backend'
  location: location
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      ingress: {
        external: false   // only reachable from within the environment
        targetPort: 8080
        transport: 'auto'
      }
      registries: [{
        server: 'ghcr.io'
        username: ghcrUsername
        passwordSecretRef: 'ghcr-token'
      }]
      secrets: [
        { name: 'ghcr-token', value: ghcrToken }
        { name: 'cosmos-connection-string', value: cosmosConnectionString }
      ]
    }
    template: {
      containers: [{
        name: 'api'
        image: '${imageBase}/backend:${imageTag}'
        env: [
          { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
          { name: 'UseInMemoryStore', value: 'false' }
          { name: 'CosmosDb__DatabaseName', value: 'taskboard' }
          // Real Azure Cosmos DB does not need SSL disabled
          { name: 'CosmosDb__DisableSslVerification', value: 'false' }
          { name: 'CosmosDb__ConnectionString', secretRef: 'cosmos-connection-string' }
        ]
        resources: { cpu: json('0.25'), memory: '0.5Gi' }
        probes: [
          {
            type: 'Readiness'
            httpGet: { path: '/api/projects', port: 8080, scheme: 'HTTP' }
            initialDelaySeconds: 5
            periodSeconds: 10
          }
          {
            type: 'Liveness'
            httpGet: { path: '/api/projects', port: 8080, scheme: 'HTTP' }
            initialDelaySeconds: 30
            periodSeconds: 20
          }
        ]
      }]
      scale: { minReplicas: 0, maxReplicas: 2 }
    }
  }
}

// ── Frontend Container App (external ingress — public HTTPS endpoint) ─────────
// nginx proxies /api/ → http://taskboard-backend, matching the internal app name above.

resource frontend 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${appName}-frontend'
  location: location
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      ingress: {
        external: true    // public HTTPS — Azure provides the TLS cert
        targetPort: 80
        transport: 'auto'
      }
      registries: [{
        server: 'ghcr.io'
        username: ghcrUsername
        passwordSecretRef: 'ghcr-token'
      }]
      secrets: [
        { name: 'ghcr-token', value: ghcrToken }
      ]
    }
    template: {
      containers: [{
        name: 'frontend'
        image: '${imageBase}/frontend:${imageTag}'
        resources: { cpu: json('0.25'), memory: '0.5Gi' }
        probes: [
          {
            type: 'Readiness'
            httpGet: { path: '/', port: 80, scheme: 'HTTP' }
            initialDelaySeconds: 5
            periodSeconds: 10
          }
          {
            type: 'Liveness'
            httpGet: { path: '/', port: 80, scheme: 'HTTP' }
            initialDelaySeconds: 15
            periodSeconds: 20
          }
        ]
      }]
      scale: { minReplicas: 0, maxReplicas: 2 }
    }
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

@description('Public HTTPS URL for the frontend')
output frontendUrl string = 'https://${frontend.properties.configuration.ingress.fqdn}'

@description('Internal URL the nginx proxy uses to reach the backend')
output backendInternalUrl string = 'http://${appName}-backend'

@description('Cosmos DB account name (for reference)')
output cosmosAccountName string = cosmosAccount.name
