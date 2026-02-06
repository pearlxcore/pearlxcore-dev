# Security Audit Report - pearlxcore.dev
**Date:** February 6, 2026  
**Status:** PARTIALLY SECURE - Implementation Required

---

## Executive Summary

The pearlxcore.dev platform demonstrates **good foundational security practices** but has several **critical issues** that require immediate attention before production deployment. The application uses secure frameworks (ASP.NET Core Identity, Entity Framework Core, Serilog) and implements some protections (CSRF tokens, password hashing, HTTPS), but has **critical gaps** in:

1. **XSS (Cross-Site Scripting)** vulnerabilities
2. **Security Headers** missing (CSP, X-Frame-Options, etc.)
3. **API Rate Limiting** not implemented
4. **Sensitive Data Exposure** in configuration files
5. **Input Validation** gaps in some areas
6. **Database Connection String** in version control

---

## Critical Issues (Must Fix)

### 1. ⚠️ CRITICAL: XSS Vulnerability in Blog Post Content

**Severity:** HIGH  
**File:** [Views/Blog/Post.cshtml](Views/Blog/Post.cshtml#L97)  
**Issue:** Post content is rendered using `@Html.Raw()` which bypasses HTML encoding:

```html
@Html.Raw(Model.RenderedContent ?? Model.Content)
```

**Risk:** If the HTML sanitizer is misconfigured or an attacker bypasses it, malicious scripts can be injected.

**Current Protection:** The [PostService.cs](Services/Implementations/PostService.cs#L227-L246) uses `Ganss.Xss.HtmlSanitizer`, but the configuration is **incomplete**:

```csharp
// ⚠️ PROBLEM: Missing important tags and too many allowed attributes
_htmlSanitizer.AllowedTags.Add("img");
_htmlSanitizer.AllowedAttributes.Add("style");  // ← Dangerous!
_htmlSanitizer.AllowedCssProperties.Add("color");
// ... but <script>, <iframe>, event handlers NOT explicitly blocked
```

**Implementation Required:** ✅
- [ ] Strengthen sanitizer configuration
- [ ] Add explicit blocklist for dangerous tags
- [ ] Disable inline event handlers (onclick, onload, etc.)
- [ ] Validate image sources (src attribute)
- [ ] Add Content Security Policy (CSP) headers

**Recommended Code:**
```csharp
private static readonly HtmlSanitizer _htmlSanitizer = new HtmlSanitizer();

static PostService()
{
    // Whitelist safe tags only
    _htmlSanitizer.AllowedTags.Clear();
    _htmlSanitizer.AllowedTags.AddRange(new[] { 
        "p", "br", "strong", "em", "u", "h1", "h2", "h3", "h4", "h5", "h6",
        "ul", "ol", "li", "blockquote", "code", "pre", "a", "img"
    });

    // Whitelist safe attributes only
    _htmlSanitizer.AllowedAttributes.Clear();
    _htmlSanitizer.AllowedAttributes.AddRange(new[] { 
        "href", "src", "alt", "title", "class"
    });

    // Disable inline styles - use CSS classes instead
    _htmlSanitizer.AllowedCssProperties.Clear();
    
    // Restrict image sources
    _htmlSanitizer.AllowedSchemesForUrl.Clear();
    _htmlSanitizer.AllowedSchemesForUrl.AddRange(new[] { "http", "https" });
}
```

---

### 2. ⚠️ CRITICAL: Missing Security Headers

**Severity:** HIGH  
**File:** [Program.cs](Program.cs)  
**Issue:** Application lacks essential HTTP security headers:

| Header | Status | Impact |
|--------|--------|--------|
| Content-Security-Policy (CSP) | ❌ Missing | Allows XSS attacks |
| X-Frame-Options | ❌ Missing | Vulnerable to clickjacking |
| X-Content-Type-Options | ❌ Missing | MIME type sniffing risk |
| Referrer-Policy | ❌ Missing | Privacy leak |
| Permissions-Policy | ❌ Missing | Feature abuse risk |
| X-XSS-Protection | ❌ Missing | Legacy XSS filter bypass |

**Implementation Required:** ✅

Add to [Program.cs](Program.cs) before `app.Run()`:

```csharp
// Add security headers middleware
app.UseMiddleware<SecurityHeadersMiddleware>();

// OR configure inline:
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
    
    // Content-Security-Policy - Adjust directives based on your assets
    context.Response.Headers.Add("Content-Security-Policy", 
        "default-src 'self'; " +
        "script-src 'self' https://cdn.jsdelivr.net https://utteranc.es; " +
        "style-src 'self' https://cdn.jsdelivr.net 'unsafe-inline'; " +
        "img-src 'self' data: https:; " +
        "frame-src https://utteranc.es; " +
        "font-src 'self' https://fonts.googleapis.com https://fonts.gstatic.com");
    
    await next();
});
```

---

### 3. ⚠️ CRITICAL: Database Credentials in Configuration

**Severity:** HIGH  
**Files:** 
- [appsettings.json](appsettings.json) - Contains SQL Server credentials
- [appsettings.Production.json](appsettings.Production.json)

**Current Issue:**
```json
"ConnectionStrings": {
    "DefaultConnection": "Server=127.0.0.1,1433;Database=pearlxcoreDevDb;User Id=sa;Password=Pxc@B525!;..."
}
```

**Risks:**
- Credentials exposed in Git repository
- All developers have access to production database
- Credentials visible in source code backups

**Implementation Required:** ✅

1. **Immediately:** Remove credentials from appsettings.json:
```json
"ConnectionStrings": {
    "DefaultConnection": "Server=${DB_SERVER};Database=${DB_NAME};User Id=${DB_USER};Password=${DB_PASSWORD};Encrypt=${DB_ENCRYPT};TrustServerCertificate=${DB_TRUST_CERT};"
}
```

2. **Add to .gitignore:**
```
appsettings.*.json
appsettings.Development.local.json
user-secrets.json
```

3. **Use User Secrets (Development):**
```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Password=..."
```

4. **Use Environment Variables (Production):**
```bash
export ASPNETCORE_ConnectionStrings__DefaultConnection="Server=..."
```

---

### 4. ⚠️ HIGH: Missing Admin Profile XSS Protection

**Severity:** MEDIUM-HIGH  
**File:** [Views/Home/About.cshtml](Views/Home/About.cshtml#L40)

**Issue:** Admin bio uses raw HTML without sanitization:
```html
@Html.Raw(Model.Bio.Replace("\n", "<br />"))
```

**Risk:** Admin can inject malicious JavaScript

**Implementation Required:** ✅

Sanitize the bio field in AdminProfileService:
```csharp
public async Task<AdminProfile> UpdateAsync(AdminProfile profile)
{
    var sanitizer = new HtmlSanitizer();
    profile.Bio = sanitizer.Sanitize(profile.Bio ?? "");
    // ... save to database
}
```

---

## High-Priority Issues

### 5. 🔴 HIGH: No Rate Limiting on Public Endpoints

**Severity:** HIGH  
**Affected Endpoints:**
- `/newsletter/subscribe` - Vulnerable to email list bombing
- `/blog/search` - Vulnerable to DoS attacks
- `/account/login` - Already has IP-based lockout (good), but no global rate limiting

**Risk:** Attackers can:
- Spam newsletter subscriptions
- Perform brute force login (though Identity lockout helps)
- Overload search endpoint

**Implementation Required:** ✅

Install NuGet package:
```powershell
dotnet add package AspNetCoreRateLimit
```

Configure in [Program.cs](Program.cs):
```csharp
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimit"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// ... in middleware configuration ...
app.UseIpRateLimiting();
```

Add to [appsettings.json](appsettings.json):
```json
"IpRateLimit": {
  "EnableEndpointRateLimiting": true,
  "StackBlockedRequests": false,
  "RealIpHeader": "X-Forwarded-For",
  "HttpStatusCode": 429,
  "IpWhitelist": ["127.0.0.1", "::1/10"],
  "EndpointWhitelist": [],
  "ClientWhitelist": [],
  "GeneralRules": [
    {
      "Endpoint": "*",
      "Period": "1m",
      "Limit": 100
    }
  ],
  "IpWhitelistPolicies": {},
  "IpBlacklistPolicies": {},
  "Endpoint": [
    {
      "Endpoint": "/newsletter/subscribe",
      "Period": "1h",
      "Limit": 5
    },
    {
      "Endpoint": "/account/login",
      "Period": "1h",
      "Limit": 10
    },
    {
      "Endpoint": "/blog/search",
      "Period": "1m",
      "Limit": 20
    }
  ]
}
```

---

### 6. 🔴 HIGH: Missing HTTPS Enforcement Headers

**Severity:** HIGH  
**File:** [Program.cs](Program.cs#L100)

**Current Code:**
```csharp
app.UseHttpsRedirection();  // ✓ Good
// BUT missing HSTS header strengthening
app.UseHsts();  // Uses default 30 days
```

**Implementation Required:** ✅

Strengthen HSTS in [Program.cs](Program.cs):
```csharp
// Configure stronger HSTS for production
if (!app.Environment.IsDevelopment())
{
    app.UseHsts(hsts => hsts
        .MaxAge(days: 365)
        .IncludeSubdomains()
        .Preload());
}
```

Also add to HTTPS redirect:
```csharp
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    // Ensure HTTPS in production
    if (context.Request.IsHttps || app.Environment.IsDevelopment())
    {
        await next();
    }
    else
    {
        var httpsUrl = $"https://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
        context.Response.Redirect(httpsUrl, permanent: true);
    }
});
```

---

## Medium-Priority Issues

### 7. 🟡 MEDIUM: Email Validation Could Be Stronger

**Severity:** MEDIUM  
**File:** [Controllers/NewsletterController.cs](Controllers/NewsletterController.cs#L73-L83)

**Current Implementation:**
```csharp
private static bool IsValidEmail(string email)
{
    try
    {
        var addr = new System.Net.Mail.MailAddress(email);
        return addr.Address == email;
    }
    catch
    {
        return false;
    }
}
```

**Issue:** Uses outdated .NET Mail validation, doesn't check for disposable emails

**Implementation Required:** ✅

Use a more robust email validation library:

```powershell
dotnet add package EmailValidation
```

```csharp
using EmailValidation;

private static bool IsValidEmail(string email)
{
    // Check format
    if (!EmailValidator.Validate(email))
        return false;
    
    // Optional: Block disposable emails
    var disposableDomains = new[] { "tempmail.com", "guerrillamail.com", "mailinator.com" };
    var domain = email.Split('@')[1];
    if (disposableDomains.Contains(domain))
        return false;
    
    return true;
}
```

---

### 8. 🟡 MEDIUM: No CSRF Token in Newsletter Form

**Severity:** MEDIUM  
**File:** [Views/Shared/_Layout.cshtml](Views/Shared/_Layout.cshtml#L70)

**Current Newsletter Form:**
```html
<form asp-controller="Newsletter" asp-action="Subscribe" method="post" class="newsletter-form">
    <!-- Missing CSRF token! -->
    <input type="email" name="email" placeholder="Enter your email" required />
    <button type="submit">Subscribe</button>
</form>
```

**Issue:** ASP.NET auto-generates CSRF tokens with `asp-*` helpers, but only if explicitly included

**Implementation Required:** ✅

Add CSRF token to form:
```html
<form asp-controller="Newsletter" asp-action="Subscribe" method="post" class="newsletter-form">
    @Html.AntiForgeryToken()
    <input type="email" name="email" placeholder="Enter your email" required />
    <button type="submit">Subscribe</button>
</form>
```

---

### 9. 🟡 MEDIUM: Insufficient Logging for Security Events

**Severity:** MEDIUM  
**Files:** [Services/Implementations/AuditLogService.cs](Services/Implementations/AuditLogService.cs)

**Issue:** Lacks logging for:
- Failed login attempts (with IP address)
- Suspicious activity patterns
- Privilege escalation attempts
- Failed authorization checks

**Implementation Required:** ✅

Enhance logging in AccountController:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Login(LoginViewModel model)
{
    if (!ModelState.IsValid)
    {
        _logger.LogWarning("Login attempt with invalid model for user: {Email}, IP: {IP}", 
            model.Email, HttpContext.Connection.RemoteIpAddress);
        return View(model);
    }

    var result = await _signInManager.PasswordSignInAsync(
        model.Email,
        model.Password,
        model.RememberMe,
        lockoutOnFailure: true
    );

    if (!result.Succeeded)
    {
        _logger.LogWarning("Failed login attempt for user: {Email}, IP: {IP}, Locked: {IsLockedOut}", 
            model.Email, HttpContext.Connection.RemoteIpAddress, result.IsLockedOut);
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    var user = await _userManager.FindByEmailAsync(model.Email);

    if (!await _userManager.IsInRoleAsync(user!, "Admin"))
    {
        _logger.LogWarning("Non-admin login attempt for user: {Email}, IP: {IP}", 
            model.Email, HttpContext.Connection.RemoteIpAddress);
        await _signInManager.SignOutAsync();
        ModelState.AddModelError(string.Empty, "Access denied.");
        return View(model);
    }

    _logger.LogInformation("Successful login for user: {Email}, IP: {IP}", 
        model.Email, HttpContext.Connection.RemoteIpAddress);
    return RedirectToLocal(model.ReturnUrl);
}
```

---

### 10. 🟡 MEDIUM: No HTTPS Only Cookie Enforcement

**Severity:** MEDIUM  
**File:** [Program.cs](Program.cs#L75-L78)

**Current Configuration:**
```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    // Missing: SameSite and HttpOnly settings
});
```

**Implementation Required:** ✅

Strengthen cookie configuration:
```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;           // Prevent JavaScript access
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // HTTPS only
    options.Cookie.SameSite = SameSiteMode.Strict;  // Prevent CSRF
    options.Cookie.Name = "PearlXCore.Auth";  // Rename from default
});
```

---

## Low-Priority Issues

### 11. 🟢 LOW: Missing OpenID/OAuth Support

**Severity:** LOW (Nice-to-have)  
**Issue:** Only supports username/password authentication

**Recommendation:** Consider adding OAuth 2.0 (GitHub, Google) for admin login to reduce password security burden.

---

### 12. 🟢 LOW: No Two-Factor Authentication (2FA)

**Severity:** LOW (Recommended)  
**File:** [Program.cs](Program.cs)

**Recommendation:** ASP.NET Identity supports 2FA out-of-the-box. Consider enabling for admin accounts:

```csharp
options.SignIn.RequireConfirmedEmail = true;
options.SignIn.RequireConfirmedPhoneNumber = false;  // Optional
```

---

### 13. 🟢 LOW: Image Upload Security

**Severity:** LOW  
**Files:** [Services/Implementations/MediaService.cs](Services/Implementations/MediaService.cs)

**Recommendations:**
- [ ] Rename uploaded files to remove original names
- [ ] Store uploads outside web root
- [ ] Validate image MIME types (not just extension)
- [ ] Implement image size limits
- [ ] Scan images for malware/malicious metadata

---

## Testing Recommendations

### Security Testing Checklist

- [ ] **OWASP Top 10 Testing** - Run OWASP ZAP or Burp Suite
- [ ] **XSS Testing** - Try injecting `<script>alert('xss')</script>` in post content
- [ ] **CSRF Testing** - Verify CSRF tokens are required
- [ ] **SQL Injection Testing** - Try quotes and SQL keywords in search/forms
- [ ] **Authentication Testing** - Verify login enforcement
- [ ] **Authorization Testing** - Verify role-based access control
- [ ] **Dependency Scanning** - Run `dotnet list package --vulnerable`

---

## Deployment Security Checklist

### Before Production

- [ ] Use environment variables for all secrets (no hardcoded passwords)
- [ ] Enable HTTPS with valid SSL certificate
- [ ] Configure HSTS header (preload recommended)
- [ ] Set up rate limiting
- [ ] Configure security headers (CSP, X-Frame-Options, etc.)
- [ ] Enable logging and monitoring
- [ ] Use strong database credentials
- [ ] Disable default accounts
- [ ] Enable audit logging
- [ ] Regular backups configured
- [ ] WAF (Web Application Firewall) deployed
- [ ] DDoS protection enabled

---

## Recommended Implementation Priority

| Priority | Issue | Effort | Impact |
|----------|-------|--------|--------|
| 🔴 P1 | XSS in post content | Medium | Critical |
| 🔴 P1 | Missing security headers | Low | Critical |
| 🔴 P1 | Credentials in config | Low | Critical |
| 🔴 P1 | Admin bio XSS | Low | High |
| 🟡 P2 | Rate limiting | Medium | High |
| 🟡 P2 | HTTPS enforcement | Low | High |
| 🟡 P2 | Email validation | Low | Medium |
| 🟡 P2 | CSRF on newsletter | Low | Medium |
| 🟡 P2 | Security logging | Medium | Medium |
| 🟡 P2 | Cookie hardening | Low | Medium |

---

## Summary

**✅ Strengths:**
- Uses ASP.NET Identity with proper password policy
- CSRF tokens implemented on admin forms
- Structured logging (Serilog)
- FluentValidation for input validation
- Entity Framework (parameterized queries = SQL injection safe)
- Account lockout protection

**❌ Weaknesses:**
- XSS vulnerabilities in content rendering
- Missing security headers
- Credentials exposed in config files
- No rate limiting
- Insufficient security logging
- Cookie security not hardened

**Recommendation:** Implement all **P1 (Critical)** items before production deployment. **P2 items** should be completed within 1-2 weeks after launch.

---

## Files to Create/Modify

1. ✏️ Modify: [Program.cs](Program.cs) - Add security headers, rate limiting, cookie hardening
2. ✏️ Modify: [Services/Implementations/PostService.cs](Services/Implementations/PostService.cs) - Strengthen HTML sanitizer
3. ✏️ Modify: [Views/Home/About.cshtml](Views/Home/About.cshtml) - Sanitize admin bio
4. ✏️ Modify: [Views/Shared/_Layout.cshtml](Views/Shared/_Layout.cshtml) - Add CSRF token to newsletter form
5. ✏️ Modify: [appsettings.json](appsettings.json) - Remove credentials, use environment variables
6. ✏️ Create: [.gitignore](../.gitignore) - Add sensitive files
7. ✏️ Modify: [Controllers/NewsletterController.cs](Controllers/NewsletterController.cs) - Improve email validation
8. ✏️ Modify: [Controllers/AccountController.cs](Controllers/AccountController.cs) - Add security logging

---

**Report Generated:** February 6, 2026  
**Status:** Ready for implementation  
**Estimated Remediation Time:** 4-6 hours
