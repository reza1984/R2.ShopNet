-- R2.ShopNet Database Initialization Script
-- Creates separate databases for each microservice

-- Identity Service
CREATE DATABASE shopnet_identity;
GRANT ALL PRIVILEGES ON DATABASE shopnet_identity TO postgres;

-- Authorization Service
CREATE DATABASE shopnet_authorization;
GRANT ALL PRIVILEGES ON DATABASE shopnet_authorization TO postgres;

-- Catalog Service
CREATE DATABASE shopnet_catalog;
GRANT ALL PRIVILEGES ON DATABASE shopnet_catalog TO postgres;

-- Inventory Service
CREATE DATABASE shopnet_inventory;
GRANT ALL PRIVILEGES ON DATABASE shopnet_inventory TO postgres;

-- Cart Service
CREATE DATABASE shopnet_cart;
GRANT ALL PRIVILEGES ON DATABASE shopnet_cart TO postgres;

-- Orders Service
CREATE DATABASE shopnet_orders;
GRANT ALL PRIVILEGES ON DATABASE shopnet_orders TO postgres;

-- Payment Service
CREATE DATABASE shopnet_payment;
GRANT ALL PRIVILEGES ON DATABASE shopnet_payment TO postgres;

-- Delivery Service
CREATE DATABASE shopnet_delivery;
GRANT ALL PRIVILEGES ON DATABASE shopnet_delivery TO postgres;

-- Warehouse Service
CREATE DATABASE shopnet_warehouse;
GRANT ALL PRIVILEGES ON DATABASE shopnet_warehouse TO postgres;

-- Notifications Service
CREATE DATABASE shopnet_notifications;
GRANT ALL PRIVILEGES ON DATABASE shopnet_notifications TO postgres;

-- Analytics Service
CREATE DATABASE shopnet_analytics;
GRANT ALL PRIVILEGES ON DATABASE shopnet_analytics TO postgres;
