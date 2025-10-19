# Quick Start Guide - Admin Portal with Aspire

## Running the Admin Portal via Aspire

### Prerequisites
✅ .NET 9.0 SDK installed  
✅ Node.js 20+ and npm installed  
✅ Docker Desktop running (for infrastructure services)  
✅ npm dependencies installed in Admin Portal

### Start Everything

```bash
# Navigate to AppHost
cd /Volumes/Secure/Projects/R2.ShopNet/src/R2.ShopNet.AppHost

# Run Aspire (starts all services including Admin Portal)
dotnet run
```

### What Happens

1. **Infrastructure Services Start:**
   - Consul (Service Discovery) → http://localhost:8500
   - PostgreSQL (Database)
   - Redis (Caching)
   - RabbitMQ (Messaging) → http://localhost:15672
   - Elasticsearch (Search)
   - And more...

2. **Identity Service Starts:**
   - API available at https://localhost:5002
   - Database migrations applied
   - Default admin user seeded

3. **Admin Portal Starts:**
   - Angular app served at http://localhost:4200
   - Hot reload enabled
   - Connected to Identity API

4. **Aspire Dashboard Opens:**
   - Auto-opens in browser at http://localhost:15XXX
   - Shows all running services
   - Real-time logs and metrics

### Access Points

| Service | URL | Credentials |
|---------|-----|-------------|
| **Admin Portal** | http://localhost:4200 | - |
| **Aspire Dashboard** | http://localhost:15XXX | Auto-opens |
| Identity API Swagger | https://localhost:5002/swagger | - |
| Consul UI | http://localhost:8500 | - |
| RabbitMQ Management | http://localhost:15672 | guest/guest |

### Using the Admin Portal

1. Open http://localhost:4200
2. You'll see the Admin Portal home page
3. Navigate to user management
4. Test features:
   - View users list
   - Search for users
   - Edit user information
   - Activate/deactivate users

### Stopping Services

Press `Ctrl+C` in the terminal where `dotnet run` is running. Aspire will gracefully stop all services.

### Troubleshooting

**Problem:** Port conflicts  
**Solution:** Stop other services using the same ports or modify ports in AppHost.cs

**Problem:** Admin Portal not loading  
**Solution:** 
```bash
cd src/Web/R2.ShopNet.Web.Admin
npm install
npm start  # Test standalone first
```

**Problem:** Docker containers won't start  
**Solution:** Ensure Docker Desktop is running

### Development Tips

1. **Hot Reload:** Angular changes reload automatically
2. **View Logs:** Check Aspire Dashboard → select service → view logs
3. **Monitor Resources:** Aspire Dashboard shows CPU/Memory usage
4. **Service Dependencies:** Aspire ensures services start in correct order

### Alternative: Run Services Individually

If you prefer not to use Aspire:

```bash
# Terminal 1: Start Identity API
cd src/Services/Identity/R2.ShopNet.Identity.API
dotnet run

# Terminal 2: Start Admin Portal
cd src/Web/R2.ShopNet.Web.Admin
npm start
```

Then manually start infrastructure via Docker Compose.

---

**Ready to go!** 🚀 Run `dotnet run` from the AppHost directory and everything starts automatically.
