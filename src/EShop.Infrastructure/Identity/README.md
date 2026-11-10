# EShop Identity & Authentication

## Overview

A comprehensive JWT-based authentication system with:
- User registration & login
- Refresh token rotation
- OAuth social login (Google, Facebook, Apple, Microsoft)
- Role-based authorization
- Email verification
- Password reset
- Account lockout protection

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                       API Layer                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                 AuthController                           │    │
│  │  /register  /login  /refresh  /logout  /me              │    │
│  │  /forgot-password  /reset-password  /confirm-email      │    │
│  │  /external-login                                        │    │
│  └─────────────────────────────────────────────────────────┘    │
├─────────────────────────────────────────────────────────────────┤
│                   Infrastructure Layer                           │
│  ┌─────────────────┐  ┌─────────────────┐  ┌────────────────┐   │
│  │ AuthService     │  │ JwtTokenService │  │ PasswordHasher │   │
│  │ OAuthValidation │  │ CurrentUser     │  │ UserRepository │   │
│  └─────────────────┘  └─────────────────┘  └────────────────┘   │
├─────────────────────────────────────────────────────────────────┤
│                      Domain Layer                                │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                User Aggregate                            │    │
│  │  - UserRole                                              │    │
│  │  - RefreshToken (single-use, rotated)                    │    │
│  │  - ExternalLogin (OAuth providers)                       │    │
│  │  - UserClaim                                             │    │
│  └─────────────────────────────────────────────────────────┘    │
│  ┌───────────────────┐  ┌─────────────────────────────────┐     │
│  │ EmailVerifyToken  │  │     PasswordResetToken          │     │
│  └───────────────────┘  └─────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────────┘
```

## Quick Start

### 1. Register a new user

```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecureP@ss123"
}
```

Response:
```json
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "base64...",
  "expiresIn": 900,
  "userId": "guid",
  "email": "user@example.com",
  "roles": ["Customer"],
  "requiresEmailConfirmation": true,
  "emailVerificationToken": "token..."
}
```

### 2. Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecureP@ss123"
}
```

### 3. Refresh Token

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "your-refresh-token"
}
```

Or use the HTTP-only cookie (set automatically).

### 4. Get Current User

```http
GET /api/auth/me
Authorization: Bearer <access_token>
```

### 5. OAuth Login (Google example)

```http
POST /api/auth/external-login
Content-Type: application/json

{
  "provider": "Google",
  "providerKey": "google-user-id",
  "email": "user@gmail.com",
  "firstName": "John",
  "lastName": "Doe"
}
```

## Configuration

### appsettings.json

```json
{
  "Jwt": {
    "Secret": "YourSuperSecretKeyHereMustBeAtLeast32CharactersLong!",
    "Issuer": "EShop.API",
    "Audience": "EShop.Client",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7,
    "RotateRefreshTokens": true,
    "ClockSkewSeconds": 0
  },
  "Password": {
    "MinimumLength": 8,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireSpecialCharacter": true,
    "MaxFailedAttempts": 5,
    "LockoutMinutes": 15
  },
  "OAuth": {
    "Google": {
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret"
    }
  }
}
```

## Roles & Authorization

### Available Roles
- `Admin` - Full system access
- `Customer` - Standard customer role (default)
- `Vendor` - Marketplace seller
- `Support` - Customer support
- `Manager` - Store manager

### Authorization Policies
- `AdminOnly` - Requires Admin role
- `VendorOrAdmin` - Requires Vendor or Admin role
- `CustomerOrAbove` - Requires Customer, Vendor, or Admin role
- `SupportOrAdmin` - Requires Support or Admin role

### Using in Controllers

```csharp
[Authorize] // Requires authentication
public class OrdersController : ControllerBase

[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase

[Authorize(Policy = "VendorOrAdmin")]
public ActionResult CreateProduct()
```

## Security Features

### Password Hashing
- PBKDF2 with SHA256
- 100,000 iterations (OWASP recommendation)
- 128-bit salt, 256-bit key
- Format: `iterations.algorithm.salt.hash`

### Refresh Token Security
- Single-use tokens (rotation on each use)
- Token reuse detection (revokes all tokens)
- Separate tokens per device (UserAgent tracking)
- Automatic cleanup of old tokens

### Account Protection
- Account lockout after N failed attempts
- Time-based lockout expiration
- Security stamp invalidation on password change
- Domain events for security monitoring

## Database Tables

| Table | Purpose |
|-------|---------|
| `users` | User accounts |
| `user_roles` | User role assignments |
| `refresh_tokens` | Refresh tokens |
| `external_logins` | OAuth provider links |
| `user_claims` | Custom claims |
| `email_verification_tokens` | Email confirmation |
| `password_reset_tokens` | Password reset |

## Domain Events

| Event | Triggered When |
|-------|----------------|
| `UserRegisteredEvent` | New user created |
| `UserLoggedInEvent` | Successful login |
| `UserLockedOutEvent` | Account locked |
| `PasswordChangedEvent` | Password updated |
| `EmailConfirmedEvent` | Email verified |
| `SuspiciousLoginActivityEvent` | Token reuse detected |

## Services

### IAuthenticationService
Main authentication operations.

### IJwtTokenService
JWT token generation and validation.

### IPasswordHasher
Secure password hashing.

### ICurrentUserService
Access current user in any service.

```csharp
public class OrderService
{
    private readonly ICurrentUserService _currentUser;
    
    public async Task CreateOrder()
    {
        var userId = _currentUser.UserId;
        var isAdmin = _currentUser.IsAdmin;
    }
}
```

### IOAuthValidationService
Validates OAuth tokens from external providers.

## Integration

### Registering Services

```csharp
// In Program.cs
builder.Services.AddIdentityServices(builder.Configuration);
```

This registers:
- All identity services
- JWT authentication
- Authorization policies
- Repositories

### Middleware Order

```csharp
app.UseAuthentication();  // Must come before
app.UseAuthorization();   // authorization
```

## Testing

Use `AddIdentityServicesForTesting()` for integration tests:

```csharp
services.AddIdentityServicesForTesting();
```

This uses relaxed password requirements and in-memory tokens.
