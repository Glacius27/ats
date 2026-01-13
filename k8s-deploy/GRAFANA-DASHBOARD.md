# Grafana Dashboard for ATS Microservices

This guide explains how to import and use the Grafana dashboard for visualizing Prometheus and .NET metrics from your ATS microservices.

## Dashboard Overview

The dashboard includes the following panels:

### HTTP Metrics
- **HTTP Request Rate** - Requests per second by service and status code
- **HTTP Request Duration** - p95 and p99 latency percentiles
- **Active HTTP Requests** - Current number of in-flight requests
- **HTTP Error Rate** - 4xx and 5xx error rates
- **HTTP Status Code Distribution** - Bar chart showing status code distribution

### .NET Runtime Metrics
- **GC Collections** - Garbage collection counts by generation
- **GC Duration** - Time spent in garbage collection
- **Process CPU Usage** - CPU usage percentage per service
- **Process Memory (Working Set)** - Memory consumption in bytes
- **Thread Count** - Number of threads per service
- **Exception Rate** - Exceptions thrown per second

### Service Health
- **Service Health Status** - Visual indicator of service availability

## Import Methods

### Method 1: Using the Import Script (Recommended)

```bash
cd k8s-deploy
./import-grafana-dashboard.sh
```

This script will:
1. Set up port-forward to Grafana
2. Import the dashboard automatically
3. Provide you with the dashboard URL

### Method 2: Manual Import via Grafana UI

1. **Access Grafana:**
   ```bash
   # Add to /etc/hosts if not already done
   echo "127.0.0.1 grafana.local" | sudo tee -a /etc/hosts
   ```
   - Open: http://grafana.local
   - Login: `admin` / `admin`

2. **Import Dashboard:**
   - Go to **Dashboards** → **Import**
   - Click **Upload JSON file**
   - Select: `observability/grafana/ats-microservices-dashboard.json`
   - Click **Load**
   - Select **Prometheus** as the datasource
   - Click **Import**

### Method 3: Using Grafana API

```bash
# Set up port-forward
kubectl port-forward -n ats svc/ats-observability-grafana 3000:80

# Import dashboard
curl -X POST \
  -H "Content-Type: application/json" \
  -u "admin:admin" \
  -d @observability/grafana/ats-microservices-dashboard.json \
  http://localhost:3000/api/dashboards/db
```

## Using the Dashboard

### Service Filter

The dashboard includes a **Service** variable at the top that allows you to:
- Filter metrics by specific service(s)
- Select "All" to view all services together
- Use multi-select to compare specific services

### Time Range

Use the time picker in the top-right to:
- Select predefined ranges (Last 5 minutes, Last 1 hour, etc.)
- Set custom time ranges
- Use relative time (e.g., "now-1h")

### Refresh Interval

The dashboard auto-refreshes every 10 seconds. You can:
- Change the refresh interval using the dropdown
- Pause auto-refresh if needed

## Prometheus Queries Reference

If you want to create custom panels, here are useful PromQL queries:

### HTTP Metrics

**Request Rate:**
```promql
sum(rate(http_requests_received_total[5m])) by (service, code)
```

**Request Duration (p95):**
```promql
histogram_quantile(0.95, sum(rate(http_request_duration_seconds_bucket[5m])) by (service, le))
```

**Active Requests:**
```promql
sum(http_requests_active) by (service)
```

**Error Rate:**
```promql
sum(rate(http_requests_received_total{code=~"5.."}[5m])) by (service)
```

### .NET Metrics

**GC Collections:**
```promql
sum(rate(dotnet_gc_collections_total[5m])) by (service, generation)
```

**Process CPU:**
```promql
sum(rate(dotnet_process_cpu_seconds_total[5m])) by (service) * 100
```

**Process Memory:**
```promql
sum(dotnet_process_working_set_bytes) by (service)
```

**Thread Count:**
```promql
sum(dotnet_threadpool_thread_count) by (service)
```

**Exception Rate:**
```promql
sum(rate(dotnet_exceptions_total[5m])) by (service)
```

## Troubleshooting

### Dashboard shows "No data"

1. **Verify Prometheus is scraping:**
   ```bash
   kubectl port-forward -n ats svc/ats-observability-kube-prometheus-prometheus 9090:9090
   # Open http://localhost:9090/targets
   ```
   Check that all services show as "UP"

2. **Verify metrics are exposed:**
   ```bash
   kubectl port-forward -n ats deployment/ats-authorization-service 8080:8080
   curl http://localhost:8080/metrics
   ```
   You should see metrics starting with `http_` and `dotnet_`

3. **Check ServiceMonitors:**
   ```bash
   kubectl get servicemonitor -n ats
   ```
   All 5 services should have ServiceMonitors

### Dashboard shows old data

- Check the time range selector
- Verify Prometheus is collecting recent data
- Check if services are running: `kubectl get pods -n ats`

### Can't access Grafana

1. **Check if Grafana is running:**
   ```bash
   kubectl get pods -n ats | grep grafana
   ```

2. **Check ingress:**
   ```bash
   kubectl get ingress -n ats | grep grafana
   ```

3. **Verify /etc/hosts:**
   ```bash
   grep grafana.local /etc/hosts
   ```

## Customizing the Dashboard

You can customize the dashboard by:

1. **Editing in Grafana UI:**
   - Open the dashboard
   - Click the gear icon (⚙️) → **Settings**
   - Click **Edit JSON** to modify the dashboard definition

2. **Exporting changes:**
   - After making changes, click **Save**
   - Go to **Settings** → **JSON Model**
   - Copy the JSON and save it to update the file

3. **Adding new panels:**
   - Click **Add** → **Visualization**
   - Select **Prometheus** as datasource
   - Enter your PromQL query
   - Configure visualization options

## Additional Resources

- [Prometheus Query Language (PromQL)](https://prometheus.io/docs/prometheus/latest/querying/basics/)
- [Grafana Dashboard Documentation](https://grafana.com/docs/grafana/latest/dashboards/)
- [prometheus-net Metrics](https://github.com/prometheus-net/prometheus-net)
