output "resource_group" {
  value = azurerm_resource_group.rg.name
}

output "acr_login_server" {
  value = azurerm_container_registry.acr.login_server
}

output "web_app_url" {
  value = azurerm_linux_web_app.api.default_hostname
}

output "postgres_fqdn" {
  value = azurerm_postgresql_flexible_server.pg.fqdn
}

output "redis_host" {
  value = azurerm_redis_cache.redis.hostname
}
