# pearlxcore.dev Codebase Guide for AI Agents

## Project Overview
**pearlxcore.dev** (née Lighthouse) is an ASP.NET Core 10 MVC blogging platform with admin panel, Identity-based authentication, newsletter management, and media handling. It uses SQL Server, Entity Framework Core, Markdig for markdown rendering, FluentValidation for model validation, and Serilog for structured logging.

**Key Stack:**
- ASP.NET Core MVC (net10.0)
- Entity Framework Core 10.0.2
- ASP.NET Identity with role-based authorization
- Markdig 0.44.0 (markdown → HTML)
- FluentValidation 11.3.1
- Serilog with daily rolling file logs
- SQL Server

---

## Architecture & Core Patterns

### Data Layer (`Data/` + `Models/Entities/`)
- **DbContext**: [ApplicationDbContext.cs](Data/ApplicationDbContext.cs) - manages Posts, Categories, Tags, PostCategory (join), PostTag (join), and AspNetIdentity tables
- **Initialization**: [DbInitializer.cs](Data/DbInitializer.cs) - runs migrations, creates Admin role, seeds admin user from `LIGHTHOUSE_ADMIN_PASSWORD` env variable
- **Key Pattern**: Fluent API configuration in `OnModelCreating()` - each entity has explicit column constraints (max length, required, indexes)

### Content Rendering Pipeline
1. **Input**: User submits Markdown content in admin panel ([PostFormViewModel.cs](ViewModels/Admin/Posts/PostFormViewModel.cs))
2. **Processing**: [PostService.CreateAsync()](Services/Implementations/PostService.cs) calls `Markdown.ToHtml(post.Content, _markdownPipeline)` using Markdig with advanced extensions
3. **Storage**: Both raw `Content` (markdown) and `RenderedContent` (HTML) stored in Post entity
4. **Display**: Public blog views render `RenderedContent` HTML directly

**Important**: Always update both fields when modifying post content. Slug generation and uniqueness handled by PostService.

### Service Layer (`Services/`)
- **Pattern**: Interface-based services registered in [Program.cs](Program.cs) as scoped dependencies (except `PostPublishingService` which is hosted background service)
- **Core Services**:
  - [IPostService](Services/Interfaces/IPostService.cs) / [PostService](Services/Implementations/PostService.cs) - posts, categories, tags, filtering by published status, slug, category, tag
  - [ICategoryService](Services/Interfaces/ICategoryService.cs) / [CategoryService](Services/Implementations/CategoryService.cs) - CRUD for categories
  - [ITagService](Services/Interfaces/ITagService.cs) / [TagService](Services/Implementations/TagService.cs) - CRUD for tags
- **New Services**:
  - [INewsletterService](Services/Interfaces/INewsletterService.cs) / [NewsletterService](Services/Implementations/NewsletterService.cs) - subscriber CRUD, unsubscribe token management
  - [IMediaService](Services/Interfaces/IMediaService.cs) / [MediaService](Services/Implementations/MediaService.cs) - image upload/delete, metadata tracking
  - [IAdminProfileService](Services/Interfaces/IAdminProfileService.cs) / [AdminProfileService](Services/Implementations/AdminProfileService.cs) - admin bio, avatar, title storage
  - [IAuditLogService](Services/Interfaces/IAuditLogService.cs) / [AuditLogService](Services/Implementations/AuditLogService.cs) - log actions with user/entity context, query by entity or time
- **Background Service**:
  - [PostPublishingService](Services/BackgroundServices/PostPublishingService.cs) - runs every 1 minute, auto-publishes posts with `ScheduledPublishAt <= DateTime.UtcNow`, registered as `AddHostedService<>`
- **Pattern**: Direct DbContext queries with `.Include()` for related entities; no repository pattern

### Controllers & Routing
- **Public Routes** ([Controllers/](Controllers/)):
  - `BlogController` - `/blog` (index), `/blog/{slug}`, `/blog/category/{slug}`, `/blog/tag/{slug}` - all filter by `IsPublished == true`
  - `HomeController` - `/` (home), `/privacy`, error handling
  - `NewsletterController` - `/newsletter/subscribe`, `/newsletter/unsubscribe/{token}` - public signup/unsubscribe
  - `AccountController` - `/account/login` - **requires Admin role**, used by admin panel only

- **Admin Routes** ([Areas/Admin/Controllers/](Areas/Admin/Controllers/)):
  - All inherit [AdminController.cs](Areas/Admin/Controllers/AdminController.cs) - marked with `[Area("Admin")]` and `[Authorize(Roles = IdentityRoles.Admin)]`
  - `PostsController` - `/admin/posts` CRUD (includes scheduled publishing)
  - `CategoriesController` - `/admin/categories` CRUD
  - `DashboardController` - `/admin` main dashboard
  - `TagsController` - `/admin/tags` CRUD
  - `NewsletterController` - `/admin/newsletter` subscriber management
  - `MediaController` - `/admin/media` image upload/delete
  - `ProfileController` - `/admin/profile` admin bio/avatar settings
  - `LogsController` - `/admin/logs` audit log viewer

**Pattern**: Admin area routes use `{area:exists}/{controller=Dashboard}/{action=Index}/{id?}` configured in [Program.cs](Program.cs#L94).

### ViewModels & Validation
- **Location**: [ViewModels/](ViewModels/) mirrors controller structure
- **Pattern**: Create separate VMs for forms, never pass domain entities directly to views
- **Validation**: [PostFormViewModelValidator](ViewModels/Admin/Posts/PostFormViewModelValidator.cs) uses FluentValidation - validates:
  - Title: required, max 200 chars
  - Slug: optional, lowercase/numbers/hyphens only
  - Content: required
- **Integration**: Validators auto-discovered via `AddValidatorsFromAssemblyContaining<Program>()` in [Program.cs](Program.cs#L25)

---

## Identity & Authorization

- **Roles**: Defined as constants in [IdentityRoles.cs](Infrastructure/IdentityRoles.cs) - currently `Admin` and `Author`
- **Admin User**: Created/seeded by [DbInitializer.cs](Data/DbInitializer.cs) - email `admin@lighthouse.local`, password from env var `LIGHTHOUSE_ADMIN_PASSWORD`
- **Identity Config** ([Program.cs](Program.cs#L35-L49)):
  - Password: 8+ chars, lowercase + digit required (no uppercase/special required)
  - Lockout: 5 failed attempts = 10 min lockout
  - Email must be unique
- **Cookie-based Auth**: Configured to redirect login to `/Account/Login` and access denied to `/Account/AccessDenied`

---

## Database & Migrations

- **Connection String**: Read from `appsettings.json` (Development) or env override - key: `DefaultConnection`
- **Auto-Migration**: DbInitializer calls `context.Database.MigrateAsync()` on startup
- **Migrations Location**: [Migrations/](Migrations/) directory
- **Key Tables**:
  - **Posts**: Title (200 chars), Slug (unique, indexed), Content (markdown), RenderedContent (HTML), IsPublished bool, PublishedAt (nullable), **ScheduledPublishAt (nullable)**, Summary (optional), ImageUrl (optional), AuthorId FK, CreatedAt, UpdatedAt
  - **Categories**: Name (100), Slug (unique, indexed)
  - **Tags**: Name (50), Slug (unique, indexed)
  - **PostCategory**, **PostTag**: Join tables for M:M relationships
  - **NewsletterSubscriber**: Email (unique), IsActive bool, SubscribedAt, UnsubscribedAt (nullable), UnsubscribeToken (nullable unique)
  - **AdminProfile**: Name, Title (optional), Bio (optional), AvatarUrl (optional), UpdatedAt
  - **AuditLog**: Action, EntityType, EntityId (nullable), Description (optional), UserId FK (nullable), CreatedAt (indexed)
  - **AspNetUsers**, **AspNetRoles**, etc.: Identity framework tables

**Pattern**: All slug fields are indexed and unique - PostService enforces `EnsureUniqueSlugAsync()` before insert/update.

---

## Development Workflow

### Build & Run
```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run (applies migrations, seeds admin user)
dotnet run

# Create migration after model changes
dotnet ef migrations add <MigrationName>
```

### Configuration
- **Development**: [appsettings.Development.json](appsettings.Development.json)
- **Production**: [appsettings.json](appsettings.json)
- **Required Env Variables** (before running):
  - `LIGHTHOUSE_ADMIN_PASSWORD` - admin user password
  - `ConnectionStrings:DefaultConnection` - optional SQL Server connection override

### Static Assets
- [wwwroot/](wwwroot/) - CSS, JS, Bootstrap/jQuery via NuGet
- Layout: [Views/Shared/_Layout.cshtml](Views/Shared/_Layout.cshtml)

---

## Important Implementation Details

### Slug Handling
- Auto-generated from Title if not provided: lowercase, spaces→hyphens, special chars removed
- Collision handling: If slug exists, appends `-1`, `-2`, etc.
- Unique constraint enforced at DB level and checked in service before insert/update

### Post Publishing Workflow
1. Draft created with `IsPublished = false`, `PublishedAt = null`, `ScheduledPublishAt = null`
2. Can be scheduled with `ScheduledPublishAt = future DateTime`
3. [PostPublishingService](Services/BackgroundServices/PostPublishingService.cs) runs every minute, checks for posts with `ScheduledPublishAt <= DateTime.UtcNow`, auto-publishes them
4. When published, `IsPublished = true`, `PublishedAt = DateTime.UtcNow`
5. Public queries filter `WHERE IsPublished = true ORDER BY PublishedAt DESC`

### Model Relationships
- **Post → Author**: 1:M, FK on AuthorId, `OnDelete.Restrict` (can't delete author with posts)
- **Post → Categories**: M:M via PostCategory join table
- **Post → Tags**: M:M via PostTag join table
- Join table updates in [PostService](Services/Implementations/PostService.cs): `UpdateCategories()` and `UpdateTags()` clear existing, add new

### Category/Tag Creation Pattern
- Simple inline form in admin views (see [CategoriesController.cs](Areas/Admin/Controllers/CategoriesController.cs#L22))
- Slug auto-generated as `name.ToLower().Replace(" ", "-")`
- No complex ViewModel, just `string name` parameter

---

## Logging & Monitoring

### Serilog Configuration
- **Setup**: Configured in [Program.cs](Program.cs#L20-L30) with daily rolling file logs
- **Log Files**: `logs/pearlxcore.dev-.txt` - rolls daily
- **Levels**: Console + file outputs, contextual enrichment enabled
- **Pattern**: All background services (e.g., [PostPublishingService](Services/BackgroundServices/PostPublishingService.cs)) use `ILogger<T>` injected logging
- **Usage**: `_logger.LogInformation("message")`, `_logger.LogError(ex, "message")` throughout services

### Audit Logging
- **Service**: [IAuditLogService](Services/Interfaces/IAuditLogService.cs) / [AuditLogService](Services/Implementations/AuditLogService.cs)
- **Methods**:
  - `LogAsync(action, entityType, entityId, description, userId)` - record an action
  - `GetRecentLogsAsync(count)` - retrieve last N actions
  - `GetLogsByEntityAsync(entityType, entityId)` - query logs for specific entity
- **Pattern**: Called from controllers to track post/category/tag changes, subscriber actions, media uploads
- **Viewer**: [LogsController](Areas/Admin/Controllers/LogsController.cs) at `/admin/logs`

---

## Newsletter Management

### System Overview
- **Subscribers Table**: [NewsletterSubscriber](Models/Entities/NewsletterSubscriber.cs) - email, IsActive, SubscribedAt, UnsubscribedAt, UnsubscribeToken
- **Service**: [INewsletterService](Services/Interfaces/INewsletterService.cs) - `SubscribeAsync()`, `UnsubscribeAsync()`, `GetByEmailAsync()`, `GetAllActiveAsync()`
- **Public Routes**:
  - `POST /newsletter/subscribe` - public signup form
  - `GET /newsletter/unsubscribe/{token}` - one-click unsubscribe (token-based, not requiring authentication)
- **Admin Routes**: `/admin/newsletter` - view subscribers, bulk delete

### Key Implementation Details
- **Unsubscribe Tokens**: Generated as unique string when user subscribes, stored with subscriber record
- **Soft Delete**: `IsActive = false` + `UnsubscribedAt` timestamp on unsubscribe (not hard delete)
- **Unique Email**: Constraint at DB level - email must be unique
- **Integration**: Newsletter endpoints are public (no auth required for subscribe/unsubscribe)

---

## Media Management

### System Overview
- **Service**: [IMediaService](Services/Interfaces/IMediaService.cs) - `UploadImageAsync()`, `DeleteImageAsync()`, `GetAllImagesAsync()`
- **Storage**: Server-based file storage (configure path in settings)
- **Admin Routes**: `/admin/media` - upload/delete images with metadata tracking
- **Usage**: Post creation form allows selecting uploaded images, stores path in Post.ImageUrl

### Key Implementation Details
- **Upload Flow**: Form upload → `UploadImageAsync()` → returns image metadata → stored in Post
- **Delete**: Physical file deletion + metadata cleanup
- **Constraints**: File type validation, size limits (configure in service)

---

## Admin Profile Management

### System Overview
- **Entity**: [AdminProfile](Models/Entities/AdminProfile.cs) - single record with Name, Title, Bio, AvatarUrl, UpdatedAt
- **Service**: [IAdminProfileService](Services/Interfaces/IAdminProfileService.cs) - `GetAsync()`, `UpdateAsync()`
- **Admin Routes**: `/admin/profile` - view/edit admin bio, avatar, title
- **Display**: Public-facing bio section uses `IAdminProfileService.GetAsync()` to fetch profile data

### Key Implementation Details
- **Single Record**: Admin profile is singleton - only one record in database
- **View Model**: [AdminProfileFormViewModel](ViewModels/Admin/Profile/AdminProfileFormViewModel.cs) for form binding
- **Pattern**: `GetAsync()` returns existing profile or creates default empty one on first call

---

## Key Files for Reference

| Purpose | File |
|---------|------|
| Service Registration & Pipeline | [Program.cs](Program.cs) |
| All Entity Models | [Models/Entities/](Models/Entities/) |
| Public Blog Routes | [Controllers/BlogController.cs](Controllers/BlogController.cs) |
| Admin Base & Authorization | [Areas/Admin/Controllers/AdminController.cs](Areas/Admin/Controllers/AdminController.cs) |
| Post CRUD (Admin) | [Areas/Admin/Controllers/PostsController.cs](Areas/Admin/Controllers/PostsController.cs) |
| Markdown Rendering Logic | [Services/Implementations/PostService.cs](Services/Implementations/PostService.cs#L8-L12) |
| Database Config & Seed | [Data/DbInitializer.cs](Data/DbInitializer.cs) |
| DbContext Schema | [Data/ApplicationDbContext.cs](Data/ApplicationDbContext.cs) |
| Newsletter Management | [Services/Implementations/NewsletterService.cs](Services/Implementations/NewsletterService.cs) |
| Media/Image Handling | [Services/Implementations/MediaService.cs](Services/Implementations/MediaService.cs) |
| Audit Log Tracking | [Services/Implementations/AuditLogService.cs](Services/Implementations/AuditLogService.cs) |
| Post Auto-Publishing | [Services/BackgroundServices/PostPublishingService.cs](Services/BackgroundServices/PostPublishingService.cs) |
| Admin Profile Settings | [Services/Implementations/AdminProfileService.cs](Services/Implementations/AdminProfileService.cs) |
| Logging Configuration | [Program.cs](Program.cs#L20-L30) |

---

## Common Tasks for AI Agents

**Adding a new Post field:**
1. Add property to [Models/Entities/Post.cs](Models/Entities/Post.cs)
2. Add Fluent API config to `ConfigurePost()` in [ApplicationDbContext.cs](Data/ApplicationDbContext.cs)
3. Create migration: `dotnet ef migrations add Add<FieldName>ToPost`
4. Update [PostFormViewModel.cs](ViewModels/Admin/Posts/PostFormViewModel.cs)
5. Update [PostFormViewModelValidator.cs](ViewModels/Admin/Posts/PostFormViewModelValidator.cs)
6. Update [PostsController](Areas/Admin/Controllers/PostsController.cs) Create/Edit actions

**Adding a new Admin feature:**
1. Create controller in [Areas/Admin/Controllers/](Areas/Admin/Controllers/) inheriting AdminController
2. Create ViewModel in [ViewModels/Admin/](ViewModels/Admin/)
3. Create validator (if needed) in [ViewModels/](ViewModels/) or subdirectory
4. Register service in [Program.cs](Program.cs) if needed
5. Views go in [Areas/Admin/Views/](Areas/Admin/Views/)<ControllerName>/

**Modifying Post content handling:**
- Edit [PostService.cs](Services/Implementations/PostService.cs) - check `CreateAsync()`, `UpdateAsync()`, and markdown pipeline configuration
- Markdown pipeline uses Markdig advanced extensions - see line 197-201 in PostService

**Adding audit logging to a feature:**
- Inject `IAuditLogService` into controller
- Call `await _auditLogService.LogAsync(action, entityType, entityId, description, userId)` after successful operations
- Example: track post creation, deletion, category changes in `PostsController`

**Integrating with Newsletter:**
- Use `INewsletterService.GetAllActiveAsync()` to fetch subscribers for bulk operations
- Generate unsubscribe tokens via `GenerateUnsubscribeTokenAsync()` helper
- Unsubscribe URL format: `{baseUrl}/newsletter/unsubscribe/{token}` (public, no auth required)

**Scheduling post publication:**
- Set `post.ScheduledPublishAt = futureDateTime` when creating/editing post
- [PostPublishingService](Services/BackgroundServices/PostPublishingService.cs) automatically publishes at scheduled time
- No polling needed - background service runs every minute checking for ready posts
