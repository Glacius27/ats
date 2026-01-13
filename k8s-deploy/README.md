# Kubernetes Deployment Guide for ATS Microservices

This directory contains scripts and configurations for deploying ATS microservices to Kubernetes (Docker Desktop).

## Observability (Prometheus + Grafana)

To set up metrics collection and visualization:

```bash
./setup-observability.sh
```

This installs Prometheus and Grafana with automatic service discovery. See [PROMETHEUS-METRICS.md](./PROMETHEUS-METRICS.md) for details.

## Prerequisites

1. **Docker Desktop** with Kubernetes enabled (NOT KIND)
2. **kubectl** - Kubernetes command-line tool
3. **Helm** - Kubernetes package manager
4. **Docker images** - Services must be built first

> **Note**: This setup is configured for Docker Desktop Kubernetes. If you're using KIND, please switch to Docker Desktop or see `switch-to-docker-desktop.md` for migration instructions.

## Quick Start

### 1. Install Ingress Controller

```bash
./install-ingress.sh
```

This installs the NGINX Ingress Controller for Docker Desktop.

### 2. Update Hosts File

Add service domains to your `/etc/hosts` file:

```bash
sudo ./update-hosts.sh
```

Or manually add these lines to `/etc/hosts`:
```
127.0.0.1 authorization.local
127.0.0.1 candidate.local
127.0.0.1 interview.local
127.0.0.1 recruitment.local
127.0.0.1 vacancy.local
```

### 3. Build Docker Images

From the project root:

```bash
./build-docker-images.sh
```

Or build individually:
```bash
docker build -f ats-authorization-service/Dockerfile -t ats-authorization-service:latest .
docker build -f ats-candidate-service/Dockerfile -t ats-candidate-service:latest .
docker build -f ats-interview-service/Dockerfile -t ats-interview-service:latest .
docker build -f ats-recruitment-service/Dockerfile -t ats-recruitment-service:latest .
docker build -f ats-vacancy-service/Dockerfile -t ats-vacancy-service:latest .
```

### 4. Deploy Services

```bash
./deploy-services.sh
```

This will:
- Create the `ats` namespace
- Deploy all services using Helm charts
- Configure ConfigMaps and Secrets
- Set up Ingress resources

### 5. Test Services

```bash
./test-services.sh
```

## Manual Deployment

### Deploy Individual Service

```bash
cd helm/ats-services
helm upgrade --install ats-authorization-service ./ats-authorization-service \
  --namespace ats \
  --set image.repository="ats-authorization-service" \
  --set image.tag="latest" \
  --set image.pullPolicy="IfNotPresent"
```

### Check Deployment Status

```bash
# Check pods
kubectl get pods -n ats

# Check services
kubectl get svc -n ats

# Check ingress
kubectl get ingress -n ats

# Check logs
kubectl logs -f deployment/ats-authorization-service -n ats
```

## Service Endpoints

Once deployed, services are accessible via:

- **Authorization Service**: http://authorization.local
- **Candidate Service**: http://candidate.local
- **Interview Service**: http://interview.local
- **Recruitment Service**: http://recruitment.local
- **Vacancy Service**: http://vacancy.local

### Swagger UI

Each service exposes Swagger UI at:
- `http://<service>.local/swagger/index.html`

### Health Checks

Health endpoints:
- `http://<service>.local/health`

## Troubleshooting

### Services not accessible

1. Check if pods are running:
   ```bash
   kubectl get pods -n ats
   ```

2. Check ingress controller:
   ```bash
   kubectl get pods -n ingress-nginx
   kubectl get svc -n ingress-nginx
   ```

3. Check ingress resources:
   ```bash
   kubectl get ingress -n ats
   kubectl describe ingress <service-name> -n ats
   ```

4. Check service logs:
   ```bash
   kubectl logs -f deployment/<service-name> -n ats
   ```

### Images not found

Docker Desktop Kubernetes uses the same Docker daemon, so images built locally are automatically available. If you see image pull errors:

1. Verify images exist:
   ```bash
   docker images | grep ats-
   ```

2. Rebuild images:
   ```bash
   cd ..
   ./build-docker-images.sh
   ```

3. Verify you're using Docker Desktop (not KIND):
   ```bash
   kubectl get nodes
   ```
   Should show `docker-desktop`, not `ats-control-plane`

### Ingress not working

1. Verify ingress controller is running:
   ```bash
   kubectl get pods -n ingress-nginx
   ```

2. Check ingress controller service:
   ```bash
   kubectl get svc -n ingress-nginx
   ```

3. Verify hosts file:
   ```bash
   cat /etc/hosts | grep local
   ```

## Cleanup

To remove all deployments:

```bash
helm uninstall ats-authorization-service -n ats
helm uninstall ats-candidate-service -n ats
helm uninstall ats-interview-service -n ats
helm uninstall ats-recruitment-service -n ats
helm uninstall ats-vacancy-service -n ats

# Remove namespace
kubectl delete namespace ats
```

## Configuration

Service configurations are managed via Helm values files:
- `helm/ats-services/<service-name>/values.yaml`

Key settings:
- **Image**: Repository, tag, and pull policy
- **Replicas**: Number of pod replicas
- **Environment Variables**: Non-sensitive config (ConfigMap)
- **Secrets**: Sensitive data (passwords, API keys)
- **Ingress**: Domain names and routing rules

## Next Steps

1. Set up persistent storage for databases
2. Configure resource limits and requests
3. Set up monitoring and logging
4. Configure TLS/HTTPS
5. Set up CI/CD pipeline
