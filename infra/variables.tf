variable "location" {
  description = "Regiao do Azure"
  type        = string
  default     = "brazilsouth"
}

variable "resource_group_name" {
  description = "Nome do Resource Group"
  type        = string
  default     = "rg-falcontouch"
}

variable "acr_name" {
  description = "Nome do Azure Container Registry (tem que ser unico no Azure)"
  type        = string
  default     = "henteqacr"
}

variable "app_service_plan_name" {
  description = "Nome do App Service Plan"
  type        = string
  default     = "asp-falcontouch-linux"
}

variable "web_app_name" {
  description = "Nome do App Service (API FalconTouch)"
  type        = string
  default     = "falcontouch-api-app"
}

variable "postgres_server_name" {
  description = "Nome do servidor PostgreSQL Flexible"
  type        = string
  default     = "falcontouch-api-app-server"
}

variable "postgres_db_name" {
  description = "Nome do banco de dados"
  type        = string
  default     = "falcontouch-api-app-database"
}

variable "postgres_admin_user" {
  description = "Usuario admin do Postgres"
  type        = string
}

variable "postgres_admin_password" {
  description = "Senha do Postgres (forte!)"
  type        = string
  sensitive   = true
}

variable "jwt_key" {
  description = "JWT signing key usada pelo FalconTouch.Api"
  type        = string
  sensitive   = true
}

variable "redis_name" {
  description = "Nome do Redis Cache"
  type        = string
  default     = "falcontouch-redis"
}

variable "redis_sku_name" {
  description = "SKU do Redis"
  type        = string
  default     = "Basic"
}

variable "redis_family" {
  description = "Familia do Redis"
  type        = string
  default     = "C"
}

variable "redis_capacity" {
  description = "Capacidade do Redis (0 = 250MB)"
  type        = number
  default     = 0
}
