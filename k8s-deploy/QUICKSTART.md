# Quick Start Guide - Deploy ATS Services to Kubernetes

## Prerequisites Checklist

- [ ] Docker Desktop installed and running
- [ ] Kubernetes enabled in Docker Desktop (Settings → Kubernetes → Enable Kubernetes)
- [ ] **NOT using KIND** - This guide is for Docker Desktop Kubernetes only
- [ ] kubectl installed (`brew install kubectl` on macOS)
- [ ] Helm installed (`brew install helm` on macOS)

> **Important**: If you're currently using KIND, you need to switch to Docker Desktop Kubernetes first. See `switch-to-docker-desktop.md` for instructions.

## Step-by-Step Deployment

### 1. Enable Kubernetes in Docker Desktop

1. Open Docker Desktop
2. Go to Settings → Kubernetes
3. Check "Enable Kubernetes"
4. Click "Apply & Restart"
5. Wait for Kubernetes to start (green indicator)

### 2. Install Ingress Controller

```bash
cd k8s-deploy
./install-ingress.sh
```

Wait for the ingress controller to be ready:
```bash
kubectl get pods -n ingress-nginx -w
```
(Press Ctrl+C when all pods show "Running")

### 3. Update Hosts File

```bash
sudo ./update-hosts.sh
```

Or manually add to `/etc/hosts`:
```
127.0.0.1 authorization.local
127.0.0.1 candidate.local
127.0.0.1 interview.local
127.0.0.1 recruitment.local
127.0.0.1 vacancy.local
```

### 4. Build Docker Images

From project root:
```bash
./build-docker-images.sh
```

This builds all service images. Verify:
```bash
docker images | grep ats-
```

### 5. Deploy Services

```bash
cd k8s-deploy
./deploy-services.sh
```

This will:
- Create the `ats` namespace
- Deploy all 5 microservices
- Set up ConfigMaps and Secrets
- Configure Ingress resources

Wait for all pods to be ready:
```bash
kubectl get pods -n ats -w
```

### 6. Test Services

```bash
./test-services.sh
```

Or test manually:
```bash
curl http://authorization.local/health
curl http://candidate.local/health
```

## Access Services

### Via Ingress (Recommended)

- Authorization: http://authorization.local
- Candidate: http://candidate.local
- Interview: http://interview.local
- Recruitment: http://recruitment.local
- Vacancy: http://vacancy.local

### Swagger UI

Each service has Swagger UI:
- http://authorization.local/swagger/index.html
- http://candidate.local/swagger/index.html
- etc.

### Direct Port Forward (Alternative)

If ingress doesn't work, use port forwarding:

```bash
kubectl port-forward -n ats svc/ats-authorization-service 8080:8080
```

Then access: http://localhost:8080

## Verify Deployment

```bash
# Check all pods
kubectl get pods -n ats

# Check services
kubectl get svc -n ats

# Check ingress
kubectl get ingress -n ats

# Check logs
kubectl logs -f deployment/ats-authorization-service -n ats
```

## Troubleshooting

### Pods not starting

```bash
# Check pod status
kubectl describe pod <pod-name> -n ats

# Check logs
kubectl logs <pod-name> -n ats
```

### Ingress not working

```bash
# Check ingress controller
kubectl get pods -n ingress-nginx

# Check ingress resources
kubectl describe ingress -n ats
```

### Images not found

Docker Desktop uses the same Docker daemon, so local images are available. If you see pull errors:

```bash
# Rebuild images
cd ..
./build-docker-images.sh
```

## Cleanup

To remove everything:

```bash
# Uninstall Helm releases
helm uninstall ats-authorization-service -n ats
helm uninstall ats-candidate-service -n ats
helm uninstall ats-interview-service -n ats
helm uninstall ats-recruitment-service -n ats
helm uninstall ats-vacancy-service -n ats

# Delete namespace
kubectl delete namespace ats
```

## Next Steps

1. Set up databases (PostgreSQL, MongoDB)
2. Configure RabbitMQ
3. Set up persistent volumes
4. Configure resource limits
5. Add monitoring
