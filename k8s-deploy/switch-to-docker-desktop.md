# Switching from KIND to Docker Desktop Kubernetes

If you're currently using KIND and want to switch to Docker Desktop Kubernetes, follow these steps:

## Step 1: Delete KIND Cluster

```bash
kind delete cluster --name ats
```

## Step 2: Enable Docker Desktop Kubernetes

1. Open Docker Desktop
2. Go to Settings → Kubernetes
3. Check "Enable Kubernetes"
4. Click "Apply & Restart"
5. Wait for Kubernetes to start (green indicator)

## Step 3: Verify Kubernetes is Running

```bash
kubectl cluster-info
kubectl get nodes
```

You should see nodes like `docker-desktop` instead of `ats-control-plane`.

## Step 4: Reinstall Ingress Controller

```bash
cd k8s-deploy
./install-ingress.sh
```

## Step 5: Deploy Services

```bash
./deploy-services.sh
```

## Benefits of Docker Desktop Kubernetes

- ✅ Simpler - no need to load images manually
- ✅ Faster - images are immediately available
- ✅ Better integration - uses your local Docker daemon
- ✅ Easier debugging - same Docker context

## Troubleshooting

If you see errors about images not found:

1. Verify images are built:
   ```bash
   docker images | grep ats-
   ```

2. Rebuild if needed:
   ```bash
   cd ..
   ./build-docker-images.sh
   ```

3. Verify Docker Desktop Kubernetes is using the same Docker daemon:
   ```bash
   docker context ls
   ```
   Should show `default` or `desktop-linux` as active.
