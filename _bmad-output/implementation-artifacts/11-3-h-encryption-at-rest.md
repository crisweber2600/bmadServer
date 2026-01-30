# Story 11.3-H: Implement Encryption at Rest for Sensitive Data

Status: ready-for-dev

## Story

As an operator,
I want sensitive data encrypted at rest,
so that data breaches don't expose plaintext.

## Acceptance Criteria

**Given** sensitive data columns are identified (RefreshToken.TokenHash is already hashed)  
**When** additional sensitive data is stored  
**Then** application-level AES-256 encryption is applied

**Given** encryption keys exist  
**When** I check configuration  
**Then** keys are loaded from environment variables  
**And** key rotation is supported without downtime

## Tasks / Subtasks

- [ ] Identify all sensitive data columns (AC: 1)
  - [ ] Audit database schema
  - [ ] Identify: workflow state with PII, artifact content, session data
  - [ ] Document encryption requirements per column
- [ ] Create DataProtection service for encryption/decryption (AC: 1, 2)
  - [ ] Use ASP.NET Core Data Protection API
  - [ ] Configure key storage location
  - [ ] Add encryption/decryption methods
- [ ] Apply to workflow state and artifact content (AC: 1)
  - [ ] Add value converters to EF Core
  - [ ] Encrypt before save, decrypt after load
  - [ ] Transparent to business logic
- [ ] Add key rotation capability (AC: 2)
  - [ ] Support multiple active keys
  - [ ] Graceful key rotation without downtime
  - [ ] Document rotation procedure
- [ ] Document encryption configuration (AC: 2)
  - [ ] Key storage requirements
  - [ ] Environment variable configuration
  - [ ] Rotation procedures
- [ ] Add tests for encryption/decryption (AC: 1, 2)
  - [ ] Test: Data encrypted before save
  - [ ] Test: Data decrypted on load
  - [ ] Test: Key rotation works
  - [ ] Test: Invalid key handled gracefully

## Dev Notes

### Architecture Context

**From:** epic-10-13-implementation-readiness-2026-01-27.md

Story 11.3-H - encryption at rest for Epic 11.

### Technical Requirements

1. **ASP.NET Core Data Protection**: Built-in encryption APIs
2. **AES-256**: Industry-standard encryption
3. **Key Management**: Environment-based, rotation support
4. **EF Core Value Converters**: Transparent encryption/decryption

### Project Structure Notes

**Files to Create:**
- `src/bmadServer.ApiService/Services/DataProtectionService.cs`
- `src/bmadServer.ApiService/Data/ValueConverters/EncryptedStringConverter.cs`
- `tests/bmadServer.Tests/Unit/Services/DataProtectionServiceTests.cs`

**Files to Modify:**
- `src/bmadServer.ApiService/Data/ApplicationDbContext.cs` - Add value converters
- `appsettings.json` - Add encryption configuration
- `src/bmadServer.ApiService/Program.cs` - Configure Data Protection

### Existing Patterns

- Entity Framework Core configured
- BCrypt for password hashing (existing)
- Value converters for JSON columns

### References

- [Source: epic-10-13-implementation-readiness-2026-01-27.md#Story 11.3-H]
- [ASP.NET Core Data Protection](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/)

## Dev Agent Record

### Agent Model Used

<!-- To be filled by dev agent -->

### File List

<!-- To be filled by dev agent -->

### Change Log

<!-- To be filled by dev agent -->
