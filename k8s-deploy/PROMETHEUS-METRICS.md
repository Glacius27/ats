# Prometheus Metrics Setup

This document describes how Prometheus metrics are configured for the ATS microservices.

## Overview

All microservices expose Prometheus metrics on the `/metrics` endpoint. These metrics are automatically discovered and scraped by Prometheus using ServiceMonitor resources.

## Metrics Endpoints

Each service exposes metrics at:
- `http://<service-name>:8080/metrics`

Available services:
- `ats-authorization-service`
- `ats-candidate-service`
- `ats-interview-service`
- `ats-recruitment-service`
- `ats-vacancy-service`

## Metrics Provided

The `prometheus-net.AspNetCore` package automatically provides:

### HTTP Metrics
- `http_requests_received_total` - Total number of HTTP requests
- `http_request_duration_seconds` - HTTP request duration histogram
- `http_requests_active` - Number of active HTTP requests
- `http_requests_received_total{code="..."}` - Requests by status code

### .NET Runtime Metrics
- `dotnet_gc_collections_total` - Garbage collection counts
- `dotnet_gc_collection_seconds_total` - GC duration
- `dotnet_process_cpu_seconds_total` - CPU usage
- `dotnet_process_working_set_bytes` - Memory usage

## Setup

### 1. Install Observability Stack

```bash
cd k8s-deploy
./setup-observability.sh
```

This installs:
- Prometheus (via kube-prometheus-stack)
- Grafana
- ServiceMonitor CRDs

### 2. Verify ServiceMonitors

```bash
kubectl get servicemonitor -n ats
```

You should see ServiceMonitors for all services.

### 3. Check Prometheus Targets

Port-forward to Prometheus:
```bash
kubectl port-forward -n ats svc/ats-observability-kube-prometheus-prometheus 9090:9090
```

Then open http://localhost:9090/targets to see all scrape targets.

### 4. Access Grafana

Add to `/etc/hosts`:
```
127.0.0.1 grafana.local
127.0.0.1 prometheus.local
```

Access Grafana:
- URL: http://grafana.local
- Username: `admin`
- Password: `admin`

## Grafana Dashboards

### Pre-configured Dashboard

A basic dashboard is automatically created at:
- **ATS Microservices Dashboard** - Shows HTTP request rates, durations, and error rates

### Creating Custom Dashboards

1. Go to Grafana → Dashboards → New Dashboard
2. Add panels with Prometheus queries:

**Request Rate:**
```promql
rate(http_requests_received_total[5m])
```

**Request Duration (p95):**
```promql
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))
```

**Error Rate:**
```promql
rate(http_requests_received_total{code=~"5.."}[5m])
```

**Active Requests:**
```promql
http_requests_active
```

## Service Configuration

Metrics are enabled by default in all services. To disable:

```yaml
# In values.yaml
metrics:
  enabled: false
```

To change scrape interval:

```yaml
metrics:
  enabled: true
  scrapeInterval: "15s"  # Default: 30s
```

## Troubleshooting

### Metrics endpoint not accessible

1. Check if metrics are enabled:
   ```bash
   kubectl get servicemonitor -n ats
   ```

2. Test metrics endpoint directly:
   ```bash
   kubectl port-forward -n ats deployment/ats-authorization-service 8080:8080
   curl http://localhost:8080/metrics
   ```

### Prometheus not scraping

1. Check ServiceMonitor labels match service labels:
   ```bash
   kubectl get servicemonitor -n ats -o yaml
   kubectl get svc -n ats -o yaml
   ```

2. Check Prometheus targets:
   ```bash
   kubectl port-forward -n ats svc/ats-observability-kube-prometheus-prometheus 9090:9090
   # Open http://localhost:9090/targets
   ```

3. Check Prometheus logs:
   ```bash
   kubectl logs -n ats -l app.kubernetes.io/name=prometheus-operator
   ```

### Grafana not showing data

1. Verify Prometheus datasource is configured:
   - Go to Grafana → Configuration → Data Sources
   - Check Prometheus datasource is present and working

2. Check if Prometheus has data:
   ```bash
   kubectl port-forward -n ats svc/ats-observability-kube-prometheus-prometheus 9090:9090
   # Open http://localhost:9090 and run a query like: http_requests_received_total
   ```

## Additional Resources

- [prometheus-net Documentation](https://github.com/prometheus-net/prometheus-net)
- [Prometheus Query Language (PromQL)](https://prometheus.io/docs/prometheus/latest/querying/basics/)
- [Grafana Dashboard Documentation](https://grafana.com/docs/grafana/latest/dashboards/)
