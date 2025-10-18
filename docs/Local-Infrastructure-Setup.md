# Local Infrastructure Setup Guide

This guide provides complete instructions for setting up the CMS platform infrastructure on self-hosted/on-premises servers.

## Table of Contents
1. [Hardware Requirements](#hardware-requirements)
2. [Software Prerequisites](#software-prerequisites)
3. [Docker Compose Setup (Simple Deployment)](#docker-compose-setup)
4. [Kubernetes Setup (Advanced Deployment)](#kubernetes-setup)
5. [Service Configuration](#service-configuration)
6. [Monitoring Stack](#monitoring-stack)
7. [Backup & Recovery](#backup--recovery)
8. [Security Hardening](#security-hardening)

---

## 1. Hardware Requirements

### Minimum Requirements (Development/Small Production)
- **CPU**: 8 cores (16 threads recommended)
- **RAM**: 32GB
- **Storage**: 250GB SSD
- **Network**: 1Gbps connectivity
- **OS**: Ubuntu Server 22.04 LTS / Debian 12 / RHEL 9

### Recommended Requirements (Production)
- **CPU**: 16+ cores (32 threads recommended)
- **RAM**: 64GB+
- **Storage**: 1TB+ NVMe SSD (RAID 10 configuration)
- **Network**: 10Gbps connectivity with redundancy
- **OS**: Ubuntu Server 22.04 LTS (long-term support)

### High Availability Setup (Multi-Server)
- **3+ Application Servers**: Load balanced
- **3+ Database Servers**: PostgreSQL cluster with replication
- **2+ Load Balancers**: HAProxy/Nginx in active-passive
- **Shared Storage**: NFS or Ceph for media files

---

## 2. Software Prerequisites

### Install Docker
```bash
# Ubuntu/Debian
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER

# Verify installation
docker --version
docker compose version
```

### Install Docker Compose (if not included)
```bash
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose
docker-compose --version
```

### Install .NET 9 SDK
```bash
# Ubuntu 22.04
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 9.0

# Add to PATH
echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.bashrc
source ~/.bashrc

dotnet --version
```

### Install Additional Tools
```bash
# Git
sudo apt update
sudo apt install -y git curl wget

# Nginx (Load Balancer / Reverse Proxy)
sudo apt install -y nginx

# Monitoring tools
sudo apt install -y htop iotop nethogs
```

---

## 3. Docker Compose Setup (Simple Deployment)

### Directory Structure
```
cms-platform/
├── docker-compose.yml
├── docker-compose.override.yml
├── .env
├── services/
│   ├── postgres/
│   │   └── init.sql
│   ├── redis/
│   │   └── redis.conf
│   ├── elasticsearch/
│   │   └── elasticsearch.yml
│   ├── rabbitmq/
│   │   └── rabbitmq.conf
│   └── minio/
│       └── config.json
├── nginx/
│   └── nginx.conf
└── backups/
```

### docker-compose.yml
```yaml
version: '3.8'

services:
  # PostgreSQL Database
  postgres:
    image: postgres:16-alpine
    container_name: cms-postgres
    restart: unless-stopped
    environment:
      POSTGRES_USER: ${POSTGRES_USER:-cmsadmin}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: cms_db
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./services/postgres/init.sql:/docker-entrypoint-initdb.d/init.sql
    ports:
      - "5432:5432"
    networks:
      - cms-network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U cmsadmin"]
      interval: 10s
      timeout: 5s
      retries: 5

  # Redis Cache
  redis:
    image: redis:7-alpine
    container_name: cms-redis
    restart: unless-stopped
    command: redis-server --appendonly yes --requirepass ${REDIS_PASSWORD}
    volumes:
      - redis_data:/data
      - ./services/redis/redis.conf:/usr/local/etc/redis/redis.conf
    ports:
      - "6379:6379"
    networks:
      - cms-network
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  # Elasticsearch
  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.11.0
    container_name: cms-elasticsearch
    restart: unless-stopped
    environment:
      - discovery.type=single-node
      - "ES_JAVA_OPTS=-Xms2g -Xmx2g"
      - xpack.security.enabled=false
      - xpack.security.enrollment.enabled=false
    volumes:
      - elasticsearch_data:/usr/share/elasticsearch/data
    ports:
      - "9200:9200"
      - "9300:9300"
    networks:
      - cms-network
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost:9200/_cluster/health || exit 1"]
      interval: 30s
      timeout: 10s
      retries: 5

  # RabbitMQ
  rabbitmq:
    image: rabbitmq:3-management-alpine
    container_name: cms-rabbitmq
    restart: unless-stopped
    environment:
      RABBITMQ_DEFAULT_USER: ${RABBITMQ_USER:-cmsadmin}
      RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASSWORD}
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    ports:
      - "5672:5672"
      - "15672:15672"
    networks:
      - cms-network
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 30s
      timeout: 10s
      retries: 5

  # MinIO (S3-compatible storage)
  minio:
    image: minio/minio:latest
    container_name: cms-minio
    restart: unless-stopped
    command: server /data --console-address ":9001"
    environment:
      MINIO_ROOT_USER: ${MINIO_ROOT_USER:-minioadmin}
      MINIO_ROOT_PASSWORD: ${MINIO_ROOT_PASSWORD}
    volumes:
      - minio_data:/data
    ports:
      - "9000:9000"
      - "9001:9001"
    networks:
      - cms-network
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:9000/minio/health/live"]
      interval: 30s
      timeout: 10s
      retries: 5

  # Prometheus (Metrics)
  prometheus:
    image: prom/prometheus:latest
    container_name: cms-prometheus
    restart: unless-stopped
    volumes:
      - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus_data:/prometheus
    ports:
      - "9090:9090"
    networks:
      - cms-network
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'

  # Grafana (Visualization)
  grafana:
    image: grafana/grafana:latest
    container_name: cms-grafana
    restart: unless-stopped
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=${GRAFANA_PASSWORD:-admin}
      - GF_USERS_ALLOW_SIGN_UP=false
    volumes:
      - grafana_data:/var/lib/grafana
    ports:
      - "3000:3000"
    networks:
      - cms-network
    depends_on:
      - prometheus

  # Loki (Log Aggregation)
  loki:
    image: grafana/loki:latest
    container_name: cms-loki
    restart: unless-stopped
    volumes:
      - loki_data:/loki
    ports:
      - "3100:3100"
    networks:
      - cms-network

  # Jaeger (Distributed Tracing)
  jaeger:
    image: jaegertracing/all-in-one:latest
    container_name: cms-jaeger
    restart: unless-stopped
    environment:
      - COLLECTOR_OTLP_ENABLED=true
    ports:
      - "16686:16686"  # UI
      - "4317:4317"    # OTLP gRPC
      - "4318:4318"    # OTLP HTTP
    networks:
      - cms-network

  # Seq (Structured Logging)
  seq:
    image: datalust/seq:latest
    container_name: cms-seq
    restart: unless-stopped
    environment:
      - ACCEPT_EULA=Y
    volumes:
      - seq_data:/data
    ports:
      - "5341:80"
    networks:
      - cms-network

volumes:
  postgres_data:
  redis_data:
  elasticsearch_data:
  rabbitmq_data:
  minio_data:
  prometheus_data:
  grafana_data:
  loki_data:
  seq_data:

networks:
  cms-network:
    driver: bridge
```

### .env File
```bash
# Database
POSTGRES_USER=cmsadmin
POSTGRES_PASSWORD=YourSecurePassword123!
POSTGRES_DB=cms_db

# Redis
REDIS_PASSWORD=YourSecureRedisPassword123!

# RabbitMQ
RABBITMQ_USER=cmsadmin
RABBITMQ_PASSWORD=YourSecureRabbitPassword123!

# MinIO
MINIO_ROOT_USER=minioadmin
MINIO_ROOT_PASSWORD=YourSecureMinioPassword123!

# Grafana
GRAFANA_PASSWORD=YourSecureGrafanaPassword123!
```

### Start Services
```bash
# Create directory structure
mkdir -p cms-platform/{services/{postgres,redis,elasticsearch,rabbitmq,minio},nginx,backups,monitoring}
cd cms-platform

# Create .env file with your passwords
nano .env

# Start all services
docker-compose up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f

# Stop services
docker-compose down

# Stop and remove volumes (WARNING: deletes data)
docker-compose down -v
```

---

## 4. Kubernetes Setup (Advanced Deployment)

### Install k3s (Lightweight Kubernetes)
```bash
# Install k3s on master node
curl -sfL https://get.k3s.io | sh -

# Get kubeconfig
sudo cat /var/lib/rancher/k3s/server/node-token
export KUBECONFIG=/etc/rancher/k3s/k3s.yaml

# Verify installation
kubectl get nodes
```

### Install Helm
```bash
curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash
helm version
```

### Deploy PostgreSQL using Helm
```bash
# Add Bitnami repo
helm repo add bitnami https://charts.bitnami.com/bitnami
helm repo update

# Install PostgreSQL
helm install cms-postgres bitnami/postgresql \
  --set auth.username=cmsadmin \
  --set auth.password=YourSecurePassword123! \
  --set auth.database=cms_db \
  --set persistence.size=50Gi

# Get connection details
export POSTGRES_PASSWORD=$(kubectl get secret --namespace default cms-postgres-postgresql -o jsonpath="{.data.postgres-password}" | base64 -d)
```

### Deploy Redis
```bash
helm install cms-redis bitnami/redis \
  --set auth.password=YourSecureRedisPassword123! \
  --set master.persistence.size=10Gi
```

### Deploy RabbitMQ
```bash
helm install cms-rabbitmq bitnami/rabbitmq \
  --set auth.username=cmsadmin \
  --set auth.password=YourSecureRabbitPassword123! \
  --set persistence.size=10Gi
```

### Deploy Elasticsearch
```bash
helm repo add elastic https://helm.elastic.co
helm install cms-elasticsearch elastic/elasticsearch \
  --set replicas=1 \
  --set volumeClaimTemplate.resources.requests.storage=50Gi
```

---

## 5. Service Configuration

### PostgreSQL Initialization
```sql
-- services/postgres/init.sql
CREATE DATABASE cms_content_db;
CREATE DATABASE cms_identity_db;
CREATE DATABASE cms_workflow_db;

-- Create application user
CREATE USER cms_app WITH PASSWORD 'YourAppPassword123!';
GRANT ALL PRIVILEGES ON DATABASE cms_content_db TO cms_app;
GRANT ALL PRIVILEGES ON DATABASE cms_identity_db TO cms_app;
GRANT ALL PRIVILEGES ON DATABASE cms_workflow_db TO cms_app;

-- Enable extensions
\c cms_content_db
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";
```

### Nginx Configuration
```nginx
# nginx/nginx.conf
upstream cms_api {
    least_conn;
    server content-service:8080;
    server content-query-service:8081;
    server media-service:8082;
}

server {
    listen 80;
    server_name cms.yourdomain.local;

    # Redirect HTTP to HTTPS
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name cms.yourdomain.local;

    ssl_certificate /etc/nginx/ssl/cert.pem;
    ssl_certificate_key /etc/nginx/ssl/key.pem;
    ssl_protocols TLSv1.3 TLSv1.2;
    ssl_ciphers HIGH:!aNULL:!MD5;

    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;

    # API Gateway
    location /api/ {
        proxy_pass http://cms_api/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # Timeouts
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }

    # Static files caching
    location /static/ {
        alias /var/www/cms/static/;
        expires 30d;
        add_header Cache-Control "public, immutable";
    }

    # Media files
    location /media/ {
        alias /var/www/cms/media/;
        expires 7d;
        add_header Cache-Control "public";
    }

    # Health check endpoint
    location /health {
        access_log off;
        return 200 "healthy\n";
    }
}
```

---

## 6. Monitoring Stack

### Prometheus Configuration
```yaml
# monitoring/prometheus.yml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'cms-services'
    static_configs:
      - targets:
          - 'content-service:8080'
          - 'content-query-service:8081'
          - 'media-service:8082'
          - 'identity-service:8083'
    metrics_path: /metrics

  - job_name: 'postgres'
    static_configs:
      - targets: ['postgres-exporter:9187']

  - job_name: 'redis'
    static_configs:
      - targets: ['redis-exporter:9121']

  - job_name: 'node'
    static_configs:
      - targets: ['node-exporter:9100']
```

### Deploy Exporters
```yaml
# Add to docker-compose.yml
services:
  postgres-exporter:
    image: prometheuscommunity/postgres-exporter
    environment:
      DATA_SOURCE_NAME: "postgresql://cmsadmin:${POSTGRES_PASSWORD}@postgres:5432/cms_db?sslmode=disable"
    ports:
      - "9187:9187"
    networks:
      - cms-network

  redis-exporter:
    image: oliver006/redis_exporter
    environment:
      REDIS_ADDR: redis:6379
      REDIS_PASSWORD: ${REDIS_PASSWORD}
    ports:
      - "9121:9121"
    networks:
      - cms-network

  node-exporter:
    image: prom/node-exporter
    ports:
      - "9100:9100"
    networks:
      - cms-network
    volumes:
      - /proc:/host/proc:ro
      - /sys:/host/sys:ro
      - /:/rootfs:ro
    command:
      - '--path.procfs=/host/proc'
      - '--path.sysfs=/host/sys'
      - '--collector.filesystem.mount-points-exclude=^/(sys|proc|dev|host|etc)($$|/)'
```

---

## 7. Backup & Recovery

### Automated Backup Script
```bash
#!/bin/bash
# backup.sh

BACKUP_DIR="/opt/cms-backups"
DATE=$(date +%Y%m%d_%H%M%S)

# PostgreSQL Backup
docker exec cms-postgres pg_dumpall -U cmsadmin | gzip > "$BACKUP_DIR/postgres_$DATE.sql.gz"

# Redis Backup
docker exec cms-redis redis-cli --pass "$REDIS_PASSWORD" SAVE
docker cp cms-redis:/data/dump.rdb "$BACKUP_DIR/redis_$DATE.rdb"

# MinIO Backup (using mc client)
docker run --rm --network cms_cms-network \
  -v "$BACKUP_DIR:/backup" \
  minio/mc alias set myminio http://minio:9000 minioadmin "$MINIO_ROOT_PASSWORD"
docker run --rm --network cms_cms-network \
  -v "$BACKUP_DIR:/backup" \
  minio/mc mirror myminio /backup/minio_$DATE

# Cleanup old backups (keep last 30 days)
find "$BACKUP_DIR" -name "*.gz" -mtime +30 -delete
find "$BACKUP_DIR" -name "*.rdb" -mtime +30 -delete

echo "Backup completed: $DATE"
```

### Schedule Backups with Cron
```bash
# Create backup directory
sudo mkdir -p /opt/cms-backups
sudo chmod 755 /opt/cms-backups

# Make script executable
chmod +x backup.sh

# Add to crontab (daily at 2 AM)
crontab -e
0 2 * * * /path/to/backup.sh >> /var/log/cms-backup.log 2>&1
```

### Restore from Backup
```bash
# Restore PostgreSQL
gunzip < /opt/cms-backups/postgres_20250117_020000.sql.gz | \
  docker exec -i cms-postgres psql -U cmsadmin

# Restore Redis
docker cp /opt/cms-backups/redis_20250117_020000.rdb cms-redis:/data/dump.rdb
docker restart cms-redis

# Restore MinIO
docker run --rm --network cms_cms-network \
  -v "/opt/cms-backups:/backup" \
  minio/mc mirror /backup/minio_20250117_020000 myminio
```

---

## 8. Security Hardening

### Firewall Configuration (UFW)
```bash
# Enable UFW
sudo ufw enable

# Allow SSH
sudo ufw allow 22/tcp

# Allow HTTP/HTTPS
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# Allow Docker Swarm (if using)
sudo ufw allow 2377/tcp
sudo ufw allow 7946/tcp
sudo ufw allow 4789/udp

# Deny all other incoming
sudo ufw default deny incoming
sudo ufw default allow outgoing

# Check status
sudo ufw status verbose
```

### SSL/TLS Certificates (Let's Encrypt)
```bash
# Install Certbot
sudo apt install -y certbot python3-certbot-nginx

# Obtain certificate
sudo certbot --nginx -d cms.yourdomain.com

# Auto-renewal
sudo systemctl enable certbot.timer
```

### Docker Security
```bash
# Enable Docker Content Trust
export DOCKER_CONTENT_TRUST=1

# Scan images for vulnerabilities
docker scan cms-content-service:latest

# Run containers as non-root
# Add to Dockerfile:
# USER nonroot:nonroot
```

### Secrets Management with HashiCorp Vault
```bash
# Run Vault in Docker
docker run -d --name vault \
  --cap-add=IPC_LOCK \
  -p 8200:8200 \
  -v vault_data:/vault/data \
  hashicorp/vault server -config=/vault/config/vault.hcl

# Initialize Vault
docker exec -it vault vault operator init

# Store secrets
docker exec -it vault vault kv put secret/cms \
  postgres_password="YourSecurePassword123!" \
  redis_password="YourSecureRedisPassword123!"
```

---

## Quick Start Commands

### Start Everything
```bash
# Clone and setup
git clone <repo-url> cms-platform
cd cms-platform

# Copy and configure environment
cp .env.example .env
nano .env  # Edit with your passwords

# Start all services
docker-compose up -d

# Check health
docker-compose ps
docker-compose logs -f

# Access services:
# - Grafana: http://localhost:3000
# - RabbitMQ Management: http://localhost:15672
# - MinIO Console: http://localhost:9001
# - Seq: http://localhost:5341
# - Jaeger: http://localhost:16686
```

### Health Checks
```bash
# PostgreSQL
docker exec cms-postgres pg_isready -U cmsadmin

# Redis
docker exec cms-redis redis-cli -a "$REDIS_PASSWORD" ping

# Elasticsearch
curl http://localhost:9200/_cluster/health?pretty

# RabbitMQ
curl -u cmsadmin:$RABBITMQ_PASSWORD http://localhost:15672/api/overview
```

---

## Troubleshooting

### View Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f postgres

# Last 100 lines
docker-compose logs --tail=100 redis
```

### Resource Usage
```bash
# Docker stats
docker stats

# System resources
htop
iotop
nethogs
```

### Reset Everything (WARNING: Deletes all data)
```bash
docker-compose down -v
docker system prune -a --volumes
```

---

**Document Version**: 1.0
**Last Updated**: 2025-10-17
**Maintained By**: DevOps Team
