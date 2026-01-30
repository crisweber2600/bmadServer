# Story 11.2-H: Add Security Headers Middleware

Status: ready-for-dev

## Story

As an operator,
I want proper security headers,
so that the application is protected from common attacks.

## Acceptance Criteria

**Given** any HTTPS response is sent  
**When** I inspect the headers  
**Then** I see:
- `Strict-Transport-Security: max-age=31536000; includeSubDomains`
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `X-XSS-Protection: 1; mode=block`
- `Content-Security-Policy: default-src 'self'; script-src 'self' 'unsafe-inline'`

**Given** HTTP request in production  
**When** request is received  
**Then** 301 redirect to HTTPS

## Tasks / Subtasks

- [ ] Add NWebsec or custom security headers middleware (AC: 1)
  - [ ] Create SecurityHeadersMiddleware.cs
  - [ ] Add all required security headers
  - [ ] Configure CSP for SPA compatibility
- [ ] Configure CSP policy appropriate for SPA (AC: 1)
  - [ ] Allow self origin
  - [ ] Allow unsafe-inline for scripts (required by some SPAs)
  - [ ] Document CSP policy decisions
- [ ] Enable HTTPS redirection in production (AC: 2)
  - [ ] Configure HTTPS redirection middleware
  - [ ] Environment-specific configuration
- [ ] Run security scanner to verify headers (AC: 1)
  - [ ] Use securityheaders.com or similar
  - [ ] Document scan results
- [ ] Add tests for header presence (AC: 1)
  - [ ] Test: All security headers present
  - [ ] Test: Header values correct
  - [ ] Test: HTTPS redirect in production

## Dev Notes

### Architecture Context

**From:** epic-10-13-implementation-readiness-2026-01-27.md

Story 11.2-H - security headers hardening for Epic 11.

### Technical Requirements

1. **HSTS**: Enforce HTTPS for 1 year
2. **Content Security Policy**: Prevent XSS attacks
3. **X-Frame-Options**: Prevent clickjacking
4. **X-Content-Type-Options**: Prevent MIME sniffing

### Project Structure Notes

**Files to Create:**
- `src/bmadServer.ApiService/Middleware/SecurityHeadersMiddleware.cs`
- `tests/bmadServer.Tests/Unit/Middleware/SecurityHeadersTests.cs`

**Files to Modify:**
- `src/bmadServer.ApiService/Program.cs` - Register middleware

### Existing Patterns

- Middleware pipeline configured in Program.cs
- Environment-based configuration available

### References

- [Source: epic-10-13-implementation-readiness-2026-01-27.md#Story 11.2-H]
- [OWASP Secure Headers](https://owasp.org/www-project-secure-headers/)

## Dev Agent Record

### Agent Model Used

<!-- To be filled by dev agent -->

### File List

<!-- To be filled by dev agent -->

### Change Log

<!-- To be filled by dev agent -->
