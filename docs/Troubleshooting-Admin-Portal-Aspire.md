# Troubleshooting: Admin Portal Not Starting in Aspire

## Common Issues and Solutions

### Issue 1: Node.js Not Found

**Symptom:** Aspire shows error "node not found" or admin-portal fails to start

**Cause:** Aspire can't find Node.js in the PATH, especially if installed via nvm

**Solutions:**

#### Option A: Run Aspire from Terminal (Recommended)
```bash
# If using nvm, ensure node is in PATH
nvm use 20

# Then run Aspire
cd src/R2.ShopNet.AppHost
dotnet run
```

#### Option B: Set Node Path Explicitly
Add to your shell profile (~/.zshrc or ~/.bashrc):
```bash
export PATH="$HOME/.nvm/versions/node/v20.19.5/bin:$PATH"
```

Then restart your terminal and run Aspire.

#### Option C: Use System Node.js
Install Node.js system-wide instead of via nvm:
```bash
brew install node
```

### Issue 2: Port Already in Use

**Symptom:** Error "Port 4200 is already in use"

**Solution:**
```bash
# Kill existing Angular processes
killall node

# Or find and kill specific process
lsof -ti:4200 | xargs kill -9
```

### Issue 3: npm Dependencies Not Installed

**Symptom:** "Cannot find module" or build errors

**Solution:**
```bash
cd src/Web/R2.ShopNet.Web.Admin
npm install
```

### Issue 4: Angular CLI Not Found

**Symptom:** "ng: command not found"

**Solution:**
The Angular CLI is installed locally in the project, which is correct. The "start" script in package.json handles this automatically.

If issues persist, try:
```bash
cd src/Web/R2.ShopNet.Web.Admin
npm install -g @angular/cli@20
```

### Issue 5: Working Directory Problems

**Symptom:** Files not found, or app starts in wrong directory

**Solution:**
The path in AppHost.cs is relative to the AppHost project:
```csharp
builder.AddNodeApp("admin-portal", "../Web/R2.ShopNet.Web.Admin", "start")
```

Verify the path is correct:
```bash
cd src/R2.ShopNet.AppHost
ls -la ../Web/R2.ShopNet.Web.Admin/package.json
```

## Verification Steps

### 1. Test Admin Portal Standalone
First, verify the app works independently:
```bash
cd src/Web/R2.ShopNet.Web.Admin
npm start
```

If this works, the issue is with Aspire integration.

### 2. Check Aspire Logs
When running Aspire:
1. Open Aspire Dashboard (auto-opens in browser)
2. Click on "admin-portal" service
3. View the console logs
4. Look for specific error messages

### 3. Verify Node.js Path
```bash
which node
which npm
node --version
npm --version
```

Should output:
- Node: v20.19.5 (or similar)
- npm: 10.x.x

### 4. Check Package.json Scripts
```bash
cd src/Web/R2.ShopNet.Web.Admin
npm run start  # Should start ng serve
```

## Alternative: Run Admin Portal Separately

If Aspire integration is problematic, you can run the admin portal separately:

### Terminal 1: Run Aspire (without admin portal)
Comment out the admin portal in AppHost.cs temporarily:
```csharp
// var adminPortal = builder.AddNodeApp("admin-portal", "../Web/R2.ShopNet.Web.Admin", "start")
//     .WithHttpEndpoint(port: 4200, env: "PORT")
//     .WithExternalHttpEndpoints()
//     .WithEnvironment("NODE_ENV", "development");
```

Then run:
```bash
cd src/R2.ShopNet.AppHost
dotnet run
```

### Terminal 2: Run Admin Portal
```bash
cd src/Web/R2.ShopNet.Web.Admin
npm start
```

## Expected Behavior

When working correctly:

1. **Aspire starts** and shows "Starting admin-portal..."
2. **npm install runs** (if needed)
3. **ng serve starts** with output like:
   ```
   ** Angular Live Development Server is listening on localhost:4200
   ** Open your browser on http://localhost:4200/
   ```
4. **Admin portal appears** in Aspire Dashboard as "Running"
5. **Logs are visible** in Aspire Dashboard

## Advanced Debugging

### Enable Verbose Logging
Add to AppHost.cs:
```csharp
var adminPortal = builder.AddNodeApp("admin-portal", "../Web/R2.ShopNet.Web.Admin", "start")
    .WithHttpEndpoint(port: 4200, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithEnvironment("NODE_ENV", "development")
    .WithEnvironment("DEBUG", "*");  // Enable debug output
```

### Check Aspire Version
```bash
dotnet --version
dotnet list package | grep Aspire
```

Ensure you're using Aspire 9.5.1 or compatible version.

### Test Node App Integration
Create a simple test to verify Node.js integration:
```csharp
// Add a simple test app
var testApp = builder.AddNodeApp("test", "../Web/R2.ShopNet.Web.Admin", "ng version")
    .WithExternalHttpEndpoints();
```

If this fails, the issue is with Node.js integration itself.

## Known Limitations

1. **Hot Reload:** Angular hot reload should work, but full restarts may be needed for some changes
2. **Build Time:** First start can be slow while npm installs dependencies
3. **Resource Usage:** Angular dev server uses significant memory

## Getting Help

If issues persist:

1. Check Aspire logs in the dashboard
2. Check Angular dev server output
3. Verify all prerequisites are installed
4. Run components separately to isolate the issue
5. Check for port conflicts

## Quick Fix Checklist

- [ ] Node.js 20+ installed and in PATH
- [ ] npm dependencies installed (`npm install`)
- [ ] Port 4200 is free
- [ ] Running from correct directory
- [ ] Angular app works standalone (`npm start`)
- [ ] Aspire can find node (`which node` works)
- [ ] No firewall blocking port 4200

---

**Most Common Solution:** Run Aspire from a terminal where Node.js is in the PATH (especially if using nvm):
```bash
nvm use 20
cd src/R2.ShopNet.AppHost
dotnet run
```
