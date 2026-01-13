# Disabling Service Discovery for Kubernetes

## Overview

The ATS microservices have been configured to use Kubernetes native DNS-based service discovery instead of the custom service discovery solution.

## Changes Made

### 1. Service Discovery Disabled

All services now have `SERVICE_DISCOVERY_ENABLED: "false"` in their Helm values:

- ✅ `ats-authorization-service` - Already disabled
- ✅ `ats-candidate-service` - Disabled
- ✅ `ats-interview-service` - Disabled  
- ✅ `ats-recruitment-service` - Disabled
- ✅ `ats-vacancy-service` - Disabled

### 2. Kubernetes DNS URLs

Services now use Kubernetes DNS names directly:

- **Authorization Service**: `http://ats-authorization-service:8080`
- **PostgreSQL**: `authorization-postgres`, `candidates-postgres`, etc.
- **RabbitMQ**: `rabbitmq:5672`
- **MongoDB**: `mongo:27017`
- **Keycloak**: `keycloak:8080`
- **Minio**: `minio:9000`

### 3. Service-to-Service Communication

Services that need to call the authorization service now use:
- `AuthService__BaseUrl: "http://ats-authorization-service:8080"`

This is set via environment variables in Helm values.

## How Kubernetes DNS Works

In Kubernetes, services are automatically discoverable via DNS:

- Service name: `ats-authorization-service`
- Namespace: `ats` (default)
- Full DNS: `ats-authorization-service.ats.svc.cluster.local`
- Short form: `ats-authorization-service` (within same namespace)

## Benefits

1. **Simpler** - No custom service discovery component needed
2. **Native** - Uses Kubernetes built-in DNS
3. **More Reliable** - No dependency on Redis/service discovery
4. **Better Performance** - Direct DNS resolution
5. **Standard Practice** - Aligns with Kubernetes best practices

## Migration Notes

The service discovery code is still in the codebase but disabled via configuration. This allows:
- Easy switch back for local development (Docker Compose)
- Gradual migration if needed
- Testing both approaches

## Local Development

For local development with Docker Compose, you can still enable service discovery by:
- Setting `SERVICE_DISCOVERY_ENABLED: "true"` in docker-compose environment variables
- Running the `ats-service-discovery` service

## Verification

To verify services are using Kubernetes DNS:

```bash
# Check environment variables
kubectl exec -n ats deployment/ats-candidate-service -- env | grep -i auth

# Test DNS resolution from within a pod
kubectl exec -n ats deployment/ats-candidate-service -- nslookup ats-authorization-service

# Check service discovery is disabled
kubectl exec -n ats deployment/ats-candidate-service -- env | grep SERVICE_DISCOVERY
```
