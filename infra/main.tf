terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }

  required_version = ">= 1.6.0"
}

provider "azurerm" {
  features {}

  subscription_id = "84f3c80b-02c4-4f4c-a12e-dd0f9e8d7edc"
}

# ---------------------------
# Resource Group
# ---------------------------
resource "azurerm_resource_group" "rg" {
  name     = var.resource_group_name
  location = var.location
}

# ---------------------------
# Container Registry (ACR)
# ---------------------------
resource "azurerm_container_registry" "acr" {
  name                = var.acr_name
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  sku                 = "Basic"
  admin_enabled       = false
}

# ---------------------------
# App Service Plan (Linux)
# ---------------------------
resource "azurerm_service_plan" "asp" {
  name                = var.app_service_plan_name
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  os_type             = "Linux"
  sku_name            = "B1"
}

# ---------------------------
# PostgreSQL Flexible Server
# (público, simples – depois dá pra colocar VNet/private link)
# ---------------------------
resource "azurerm_postgresql_flexible_server" "pg" {
  name                   = var.postgres_server_name
  resource_group_name    = azurerm_resource_group.rg.name
  location               = azurerm_resource_group.rg.location
  administrator_login    = var.postgres_admin_user
  administrator_password = var.postgres_admin_password

  sku_name   = "B_Standard_B1ms"
  storage_mb = 32768
  version    = "16"

  authentication {
    active_directory_auth_enabled = false
    password_auth_enabled         = true
  }

  # Acesso público + permite serviços do Azure
  public_network_access_enabled = true

  depends_on = [azurerm_resource_group.rg]
}

resource "azurerm_postgresql_flexible_server_database" "pgdb" {
  name      = var.postgres_db_name
  server_id = azurerm_postgresql_flexible_server.pg.id
  charset   = "UTF8"
  collation = "en_US.utf8"
}

# Firewall liberando tudo (ajusta depois!)
resource "azurerm_postgresql_flexible_server_firewall_rule" "allow_all" {
  name             = "allow-all-ip"
  server_id        = azurerm_postgresql_flexible_server.pg.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "255.255.255.255"
}

# ---------------------------
# Redis Cache (opcional, mas mapeado no appsettings)
# ---------------------------
resource "azurerm_redis_cache" "redis" {
  name                = var.redis_name
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  capacity            = var.redis_capacity
  family              = var.redis_family
  sku_name            = var.redis_sku_name

  minimum_tls_version = "1.2"
}

# String de conexão para Redis
locals {
  redis_connection_string = "${azurerm_redis_cache.redis.hostname}:${azurerm_redis_cache.redis.port},password=${azurerm_redis_cache.redis.primary_access_key},ssl=True,abortConnect=False"
}

# ---------------------------
# App Service (API .NET)
# ---------------------------
resource "azurerm_linux_web_app" "api" {
  name                = var.web_app_name
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  service_plan_id     = azurerm_service_plan.asp.id

  identity {
    type = "SystemAssigned"
  }

  site_config {
    # Imagem do ACR (você sobe a imagem separadamente)
    linux_fx_version = "DOCKER|${azurerm_container_registry.acr.login_server}/falcontouch-api:latest"

    # Essa flag lembra o App Service que vai usar managed identity pra puxar do ACR
    container_registry_use_managed_identity = true

    always_on = true
  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT" = "Production"

    # JWT (o Program.cs lê Jwt:Key & Jwt:Issuer via configuration)
    "Jwt__Key"    = var.jwt_key
    "Jwt__Issuer" = "FalconTouch.Api"

    # Swagger toggle (se você usar no Program)
    "Swagger_Enabled" = "true"

    # Para rodar migrations assim como local
    "DOTNET_ENVIRONMENT" = "Production"
  }

  # ConnectionStrings do custom configuration .NET
  connection_string {
    name  = "Postgres"
    type  = "PostgreSQL"
    value = "Host=${azurerm_postgresql_flexible_server.pg.fqdn};Port=5432;Database=${azurerm_postgresql_flexible_server_database.pgdb.name};Username=${var.postgres_admin_user};Password=${var.postgres_admin_password};Ssl Mode=Require;Trust Server Certificate=True"
  }

  connection_string {
    name  = "Redis"
    type  = "Custom"
    value = local.redis_connection_string
  }

  depends_on = [
    azurerm_postgresql_flexible_server.pg,
    azurerm_postgresql_flexible_server_database.pgdb,
    azurerm_redis_cache.redis,
  ]
}

# ---------------------------
# Role assignment: AppService pode puxar imagem do ACR
# ---------------------------
data "azurerm_subscription" "current" {}

resource "azurerm_role_assignment" "acr_pull" {
  scope                = azurerm_container_registry.acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_linux_web_app.api.identity[0].principal_id
}
