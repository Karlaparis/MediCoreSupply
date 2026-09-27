# MediCoreSupply Backend: How It Was Built

This guide walks through how the MediCoreSupply API was built, from an empty project to a live deployment on Azure. Part 1 covers the ASP.NET Core API and EF Core database layer. Part 2 covers the Azure resources and deployment.

**Stack:** ASP.NET Core 8 Web API, Entity Framework Core 8 (SQL Server provider), Swagger (Swashbuckle), Azure SQL Database, Azure App Service (Linux).

---

## Part 1: The API and database (EF Core)

### Phase 1: Project setup

1. **Created the Web API project** `backend/MediCoreSupply.Api` (ASP.NET Core 8) inside `backend/MediCoreSupply.sln`. The template provides `Program.cs`, `appsettings.json` and `Properties/launchSettings.json`.
2. **Added the NuGet packages:**

   | Package | Purpose |
   |---|---|
   | `Microsoft.EntityFrameworkCore.SqlServer` | EF Core and the SQL Server driver |
   | `Microsoft.EntityFrameworkCore.Design` | Used by the `dotnet ef` migration commands at design time. Marked `PrivateAssets=all`, so it is not shipped with the app. |
   | `Swashbuckle.AspNetCore` | Swagger: generated API documentation and a test page |

### Phase 2: Domain model and database design

3. **Entities** in `Domain/Entities` are plain C# classes that become tables: `Category`, `Product`, `Warehouse`, `InventoryItem`, `Customer`, `Order` and `OrderItem`, plus the enums `CustomerType` and `OrderStatus`. Navigation properties such as `Product.Category` and `Order.OrderItems` define the relationships.
4. **`MediCoreSupplyDbContext`** (`Data/MediCoreSupplyDbContext.cs`) is the class EF Core works through:
   - one `DbSet<T>` per table (`Categories`, `Products`, ...);
   - a constructor that takes `DbContextOptions<MediCoreSupplyDbContext>`, so dependency injection supplies the connection settings instead of the class hard-coding them.
5. **Model rules in `OnModelCreating`** use the Fluent API for rules that plain classes cannot express:
   - **Required fields and maximum lengths**, for example `Name` at 200 characters and `Sku` at 64.
   - **Unique indexes** on the category name, product SKU and order number, and on the (product, warehouse) pair for inventory.
   - **Money columns** stored as `decimal(18,2)`.
   - **Enums stored as text** (`"Hospital"`, `"Pending"`) instead of numbers, so the data can be read directly in the database.
   - **Delete behavior:**

     | Relationship | Rule | Effect |
     |---|---|---|
     | Category → Products | `Restrict` | A category that has products cannot be deleted. |
     | Warehouse → InventoryItems | `Restrict` | A warehouse that has inventory cannot be deleted. |
     | Customer → Orders | `Restrict` | A customer with orders cannot be deleted. |
     | Product → OrderItems | `Restrict` | A product that appears on orders cannot be deleted. |
     | Product → InventoryItems | `Cascade` | Deleting a product deletes its inventory rows. |
     | Order → OrderItems | `Cascade` | Deleting an order deletes its lines. |

   - `OrderItem.UnitPrice` stores the product's price at the time of the order, so later price changes don't rewrite past orders.
6. **First migration:** `dotnet ef migrations add InitialCreate` compared the model to an empty database and generated the C# code that creates every table, key and index (`Migrations/20260906180506_InitialCreate.cs`). `MediCoreSupplyDbContextModelSnapshot.cs` records the current model, so future migrations contain only the changes.

### Phase 3: Wiring it together in `Program.cs` (dependency injection and configuration)

7. **Services registered in the DI container:**

   ```csharp
   builder.Services.AddControllers()
       .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
   builder.Services.AddEndpointsApiExplorer();
   builder.Services.AddSwaggerGen();

   builder.Services.AddDbContext<MediCoreSupplyDbContext>(options =>
       options.UseSqlServer(
           builder.Configuration.GetConnectionString("DefaultConnection"),
           sqlOptions => sqlOptions.EnableRetryOnFailure()));
   ```

   - `JsonStringEnumConverter` makes the API send and accept enums as text (`"Clinic"`, not `1`).
   - `AddDbContext` registers the DbContext as a **scoped** service: one instance per HTTP request, created and disposed by the framework.
8. **Connection string:** `appsettings.json` holds `ConnectionStrings:DefaultConnection`, which points at a local SQL Server (`localhost`, Windows authentication). Other configuration sources override it (see [How configuration flows](#how-configuration-flows)).
9. **Controllers:** one per resource (`CategoriesController`, `ProductsController`, ...). Each receives the DbContext **through its constructor**, and ASP.NET Core supplies that request's instance:

   ```csharp
   public CategoriesController(MediCoreSupplyDbContext context)
   {
       _context = context;
   }
   ```

10. **DTOs:** requests (`CreateXxxRequest`, `UpdateXxxRequest`) and responses (`XxxResponse`) are separate from the entities, so the API never exposes database classes directly. Validation attributes on the requests (`[Required]`, `[MaxLength]`, `[Range]`, `[EmailAddress]`) are checked automatically by `[ApiController]`, which returns a 400 before the action runs.
11. **Query patterns:**
    - `AsNoTracking()` for read-only queries, which is faster because EF doesn't track changes;
    - `Select(...)` builds the response DTO inside the database query, so only the needed columns are read;
    - a `CancellationToken` on every database call, so the work stops if the client disconnects.
12. **Migrations and seeding at startup, in Development only:**

    ```csharp
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MediCoreSupplyDbContext>();
        await db.Database.MigrateAsync();   // applies any migrations not yet applied
        await DbSeeder.SeedAsync(db);       // inserts sample data if the database is empty
    }
    ```

    `CreateScope()` is required because the DbContext is scoped. Outside an HTTP request there is no scope, so the code creates one.

### The seeder (`Data/DbSeeder.cs`)

13. `DbSeeder.SeedAsync` fills an empty database with synthetic sample data: 4 categories, 8 products, 2 warehouses, 9 inventory rows, 3 customers (a hospital, a clinic and a pharmacy) and 3 orders (one Delivered, one Shipped, one Pending).
    - Objects are linked through navigation properties (`Category = ppe`, `Customer = generalHospital`), so EF fills in all IDs and foreign keys.
    - `AddRange` only tracks the objects in memory. A single `SaveChangesAsync()` at the end writes everything **in one transaction**: either all rows are saved or none are.
    - A guard at the top (`if (await context.Categories.AnyAsync()) return;`) makes it safe to run on every startup without duplicating data. It checks only the Categories table.
    - The data is persisted like any other data. It stays until the database is deleted.

### Phase 4: Error handling

14. **Global exception handler** (`Infrastructure/GlobalExceptionHandler.cs`) uses .NET 8's `IExceptionHandler`:

    ```csharp
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
    ...
    app.UseExceptionHandler();
    ```

    It receives `ILogger` and `IHostEnvironment` through DI. It logs every unhandled exception and returns a standard `ProblemDetails` 500 response. The stack trace is included only in Development.
15. **Expected errors are handled in the controllers:**

    | Status | When |
    |---|---|
    | 400 Bad Request | Validation failed, or a referenced record does not exist (for example an unknown category) |
    | 404 Not Found | Unknown ID |
    | 409 Conflict | Duplicate name or SKU (caught as `DbUpdateException` with SQL error 2601 or 2627, the unique-index violations), or a delete blocked because the record is still in use |

### Phase 5: Switching to Azure SQL

16. **User secrets:** `dotnet user-secrets init` added a `UserSecretsId` to the `.csproj`, and `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<Azure connection string>"` stored the Azure connection string in the Windows user profile, outside the repo. In Development it overrides `appsettings.json`, so no code changed.
17. **`EnableRetryOnFailure()`:** the free serverless database pauses when idle. The first query after a pause fails with a temporary error (40613) while the database resumes, and this option makes EF retry instead of failing.
18. **Created the Azure schema and data from a local run:** running the app locally against Azure applied `InitialCreate` and seeded the Azure database (step 12).

### Phase 6: Full CRUD and deployment readiness

19. **Update and delete endpoints** (`PUT`, `DELETE`) on Categories, Warehouses, Products, Customers and Inventory, following the delete rules from step 5:
    - A delete that would break a reference returns 409 with a readable message instead of a database error.
    - Products and customers with order history should be marked inactive (`isActive: false`) instead of deleted.
    - Inventory updates change stock levels only; a different product or warehouse is a different record.
    - Orders have no update or delete. They are cancelled through `PATCH /api/orders/{id}/status`.
20. **Swagger in every environment**, and `/` redirects to `/swagger`. Until the frontend exists, Swagger is the public demo.
21. **Production behavior:** migrations and seeding do not run outside Development, so the deployed app never changes the database schema by itself.

### How configuration flows

```
appsettings.json   →   user secrets (Development only)   →   environment variables (Azure)
  (localhost)              (Azure SQL, locally)                 (Azure SQL, deployed)

                    later sources override earlier ones
                                    ↓
                GetConnectionString("DefaultConnection")
                                    ↓
          AddDbContext  →  DI container  →  controllers' constructors
```

| Setting | Local (`dotnet run`) | Azure App Service |
|---|---|---|
| Environment | `Development` (from `launchSettings.json`) | `Production` (the default) |
| Connection string | User secrets | Web app → Environment variables → Connection strings |
| Swagger | On | On |
| Migrations and seeding at startup | Yes | No |
| Error details in 500 responses | Full stack trace | Hidden (logged only) |

---

## Part 2: Azure resources and deployment

### Foundation

1. **Azure free trial subscription.** This is the billing account: trial credit plus always-free services.
2. **Resource group `rg-medicore-dev`.** A folder that holds every resource for this project, so they can be managed or deleted together.

### Database: Azure SQL

Created in one step with the Portal's **Create SQL database** wizard, using the free offer. The server was created with **Create new** inside the same form. East US and East US 2 refused new SQL servers on the trial subscription; West US worked.

3. **Logical SQL server `sql-medicore-mcs03`** (West US). The container for databases: an address (`sql-medicore-mcs03.database.windows.net`), an admin login using SQL authentication, and firewall rules.
4. **Database `free-sql-db-4810110`.** General Purpose, Serverless, Gen5, with the free offer applied: 100,000 vCore-seconds and 32 GB of storage per month. When the free amount runs out, the database pauses until the next month, so it never bills.
5. **Firewall rule `AllowMyIP`.** Azure SQL blocks all connections by default. This rule lets the developer's home IP connect, for local runs and migrations.
6. **Exception "Allow Azure services and resources to access this server".** Lets services running inside Azure, such as the web app, reach the database.

### Hosting: Azure App Service

Created in one step with the Portal's **Create Web App** wizard. The plan was created with **Create new** inside the same form. West US and West US 2 had a Free F1 quota of 0 on the trial subscription; West US 3 worked.

7. **App Service plan `asp-medicore-dev`.** Linux, Free F1 tier, West US 3. This is the server capacity and the part that is billed (free on F1). One plan can run several web apps.
8. **Web app `app-medicore-mcs01`.** .NET 8 (LTS) on Linux. Basic authentication is off (deployments sign in with the developer's Azure account), and Application Insights is off.
9. **Connection string on the web app:** Environment variables → Connection strings → `DefaultConnection`, type `SQLAzure`. Azure passes it to the app as an environment variable, and ASP.NET Core reads it with the same `GetConnectionString("DefaultConnection")` call. The name must match exactly.
10. **Container logging** (`az webapp log config --docker-container-logging filesystem`). Saves the app's console output so errors can be read with `az webapp log tail`.

### Parent and child resources

| | Parent (container or capacity) | Child (the thing you use) |
|---|---|---|
| Database | SQL server `sql-medicore-mcs03` | Database `free-sql-db-4810110` |
| Hosting | App Service plan `asp-medicore-dev` | Web app `app-medicore-mcs01` |

### Deploying

11. **`backend/deploy-api.ps1`**, run from the repo root with `.\backend\deploy-api.ps1`:
    1. deletes the previous build folder and zip;
    2. `dotnet publish -c Release`;
    3. zips the build with `tar`;
    4. `az webapp deploy --type zip --clean true`, which empties the site folder before copying.

    It requires the .NET SDK and the Azure CLI, signed in with `az login`.

### Lessons learned from the first deployment

- **Windows PowerShell's `Compress-Archive` writes zip paths with backslashes** (`runtimes\unix\...`). The Linux App Service rejected the upload (HTTP 400), and the partial deployment left out `runtimes/unix/lib/net6.0/Microsoft.Data.SqlClient.dll`. The app started and Swagger loaded, but every database call failed with `FileNotFoundException`. The fix was to zip with `tar`, which writes standard forward-slash paths, and deploy with `--clean true`.
- **The production error handler hides details on purpose.** The real exception is in the logs (`az webapp log tail`), not in the HTTP response.
- **Configuration problems appear on the first database call, not at startup.** A missing or misnamed connection string still lets the app start.

### Architecture

```
Browser ──► App Service: app-medicore-mcs01 (West US 3, Linux, F1)
                 │  DefaultConnection
                 ▼
            Azure SQL: sql-medicore-mcs03 / free-sql-db-4810110 (West US, serverless, free offer)
```

---

## Next steps

- Make the live API read-only (or protect write endpoints) before sharing the link publicly.
- Tighten the order status workflow (no backward moves) and reject orders for inactive customers.
- Move the connection string into Azure Key Vault and use a Managed Identity.
- Blob Storage, an Azure Functions project, and Application Insights.
- Upgrade from .NET 8 (end of support November 2026) to .NET 10.
