# Story 1.5: Set Up Prometheus and Grafana Monitoring Stack

**Status:** ready-for-dev

## Story

As an operator,
I want to monitor system health, API metrics, and database performance,
so that I can detect issues and debug problems.

## Acceptance Criteria

**Given** I have the `docker-compose.yml` from Story 1.3  
**When** I add `prometheus:2.45` and `grafana:10` services  
**Then** the prometheus service includes:
  - Exposed on port 9090
  - Config file: `prometheus.yml`
  - Health check: `/-/healthy`  
**And** the grafana service includes:
  - Exposed on port 3001
  - Admin credentials: admin/admin (local only)
  - Health check: `/api/health`
  - Auto-provision datasources from `/etc/grafana/provisioning`

**Given** prometheus service is running  
**When** I create `prometheus.yml` config  
**Then** it includes:
  - global: { scrape_interval: 15s }
  - scrape_configs targeting http://bmadserver:8080/metrics
  - Job name: bmadserver_api

**Given** the API service is configured to export metrics  
**When** I add `dotnet add package Prometheus.Client`  
**Then** the package installs in `bmadServer.ApiService` without errors

**Given** Prometheus.Client is installed  
**When** I add metrics initialization to `Program.cs`  
**Then** the code includes:
  - app.UseMetricServer() to expose /metrics endpoint
  - Metrics for: HTTP requests, response times, errors
  - Custom metrics for: active workflows, agents, decisions

**Given** the API exports metrics  
**When** I run `docker-compose up`  
**Then** Prometheus scrapes metrics from http://bmadserver:8080/metrics  
**And** Grafana starts and connects to Prometheus datasource  
**And** I can access Grafana at http://localhost:3001

**Given** Grafana is running  
**When** I log in with admin/admin  
**Then** I see Prometheus datasource is configured  
**And** default dashboards appear (if pre-provisioned)

**Given** Grafana is configured  
**When** I create a basic dashboard  
**Then** it includes:
  - Graph: "HTTP Request Rate" (requests/sec over time)
  - Graph: "Response Time (p50, p95, p99)"
  - Graph: "Error Rate" (5xx errors)
  - Graph: "Active Connections"
  - Gauge: "Database Connection Pool Usage"

**Given** the monitoring stack is complete  
**When** I trigger an API request: GET /health  
**Then** I can see the metric appear in Prometheus  
**And** the metric renders in Grafana within 15 seconds  
**And** the request count increments in the dashboard

**Given** monitoring is set up  
**When** I run `docker-compose ps`  
**Then** I see:
  - bmadserver (healthy)
  - postgres (healthy)
  - prometheus (healthy)
  - grafana (healthy)

## Tasks / Subtasks

- [ ] **Task 1: Add Prometheus and Grafana to docker-compose** (AC: #1)
  - [ ] Add prometheus:2.45 service
  - [ ] Configure port 9090 mapping
  - [ ] Mount prometheus.yml config file
  - [ ] Add health check for /-/healthy
  - [ ] Add grafana:10 service
  - [ ] Configure port 3001 mapping
  - [ ] Set admin credentials
  - [ ] Mount provisioning directory
  - [ ] Add health check for /api/health

- [ ] **Task 2: Create Prometheus configuration** (AC: #2)
  - [ ] Create monitoring/prometheus.yml
  - [ ] Set global scrape_interval: 15s
  - [ ] Add scrape_config for bmadserver_api
  - [ ] Target http://bmadserver:8080/metrics
  - [ ] Test configuration validity

- [ ] **Task 3: Add Prometheus metrics to API** (AC: #3-4)
  - [ ] Add prometheus-net.AspNetCore package
  - [ ] Configure UseHttpMetrics() middleware
  - [ ] Add app.UseMetricServer() for /metrics endpoint
  - [ ] Create custom metrics (CounterFactory, HistogramFactory)
  - [ ] Test /metrics endpoint returns Prometheus format

- [ ] **Task 4: Configure Grafana datasource provisioning** (AC: #5-6)
  - [ ] Create monitoring/grafana/provisioning/datasources/
  - [ ] Create datasource.yml for Prometheus
  - [ ] Set URL to http://prometheus:9090
  - [ ] Enable as default datasource
  - [ ] Test Grafana connects to Prometheus

- [ ] **Task 5: Create basic monitoring dashboard** (AC: #7)
  - [ ] Create monitoring/grafana/provisioning/dashboards/
  - [ ] Create dashboard.json with key panels
  - [ ] Add HTTP Request Rate panel
  - [ ] Add Response Time percentiles panel
  - [ ] Add Error Rate panel
  - [ ] Add Active Connections gauge
  - [ ] Add Database Pool gauge (placeholder)

- [ ] **Task 6: Verify end-to-end monitoring** (AC: #8-9)
  - [ ] Run docker-compose up
  - [ ] Make test requests to API
  - [ ] Verify metrics appear in Prometheus
  - [ ] Verify metrics render in Grafana
  - [ ] Verify all containers healthy
  - [ ] Document monitoring URLs

## Dev Notes

### Docker Compose Additions

```yaml
  prometheus:
    image: prom/prometheus:v2.45.0
    container_name: bmadserver_prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - prometheus_data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
    healthcheck:
      test: ["CMD", "wget", "-q", "--spider", "http://localhost:9090/-/healthy"]
      interval: 10s
      timeout: 5s
      retries: 3

  grafana:
    image: grafana/grafana:10.0.0
    container_name: bmadserver_grafana
    ports:
      - "3001:3000"
    environment:
      - GF_SECURITY_ADMIN_USER=admin
      - GF_SECURITY_ADMIN_PASSWORD=admin
      - GF_USERS_ALLOW_SIGN_UP=false
    volumes:
      - ./monitoring/grafana/provisioning:/etc/grafana/provisioning:ro
      - grafana_data:/var/lib/grafana
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:3000/api/health"]
      interval: 10s
      timeout: 5s
      retries: 3
    depends_on:
      - prometheus

volumes:
  prometheus_data:
  grafana_data:
```

### Prometheus Configuration

```yaml
# monitoring/prometheus.yml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'bmadserver_api'
    static_configs:
      - targets: ['bmadserver:8080']
    metrics_path: /metrics
```

### Grafana Datasource Provisioning

```yaml
# monitoring/grafana/provisioning/datasources/datasource.yml
apiVersion: 1
datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    isDefault: true
```

### Architecture Alignment

Per architecture.md requirements:
- Monitoring: Prometheus 2.45+ + Grafana 10+ ✅
- Health Checks: Built-in endpoint ✅
- Metrics: HTTP requests, response times, custom business metrics ✅

### Dependencies

- **Depends on**: Story 1-1 (API project), Story 1-3 (Docker Compose)
- **Enables**: Production monitoring, alerting, SLA tracking

### References

- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Documentation](https://grafana.com/docs/)
- [prometheus-net](https://github.com/prometheus-net/prometheus-net)
- [Epic 1 Story 1.5](../../planning-artifacts/epics.md#story-15-set-up-prometheus-and-grafana-monitoring-stack)

## Dev Agent Record

### Agent Model Used

Claude 3.5 Sonnet

### Completion Notes List

- Story created with full acceptance criteria
- Docker Compose additions documented
- Prometheus and Grafana configs included

### File List

- /Users/cris/bmadServer/docker-compose.yml (modify)
- /Users/cris/bmadServer/monitoring/prometheus.yml (create)
- /Users/cris/bmadServer/monitoring/grafana/provisioning/datasources/datasource.yml (create)
- /Users/cris/bmadServer/monitoring/grafana/provisioning/dashboards/dashboard.yml (create)
- /Users/cris/bmadServer/monitoring/grafana/provisioning/dashboards/bmadserver.json (create)
- /Users/cris/bmadServer/bmadServer.ApiService/Program.cs (modify)
