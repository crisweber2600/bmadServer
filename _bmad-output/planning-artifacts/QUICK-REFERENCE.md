# bmadServer - Quick Reference Guide for Team

**Status:** ✅ READY FOR IMPLEMENTATION  
**Generated:** January 23, 2026  
**Next Phase Starts:** February 3, 2026 (Week 1)

---

## 🎯 Where to Find Everything

All planning artifacts are located in:
```
/Users/cris/bmadServer/_bmad-output/planning-artifacts/
```

### Core Architecture Documents (START HERE)
1. **architecture.md** (163 KB) - Read first, covers all 25 decisions
2. **STEP-5-COMPLETION-REPORT.md** (19 KB) - Overview of what was done
3. **adr/** folder - 4 ADR files with decision details and code examples

### Implementation Guides (FOR DEVELOPERS)
1. **implementation-patterns.md** (42 KB) - 50+ copy-paste code examples
2. **developer-onboarding.md** (17 KB) - 30-minute setup guide
3. **project-context-ai.md** (14 KB) - Rules for AI agents & code review

### Execution Plans (FOR MANAGERS)
1. **8-week-roadmap.md** (24 KB) - Week-by-week sprint breakdown
2. **prd.md** (18 KB) - Product requirements
3. **product-brief-bmadServer-2026-01-20.md** (16 KB) - Product context

### UX/Design
1. **ux-design-specification.md** (30 KB) - UI/UX patterns and mockups

---

## 🏗️ Technology Stack (LOCKED - DO NOT CHANGE)

### Backend
- ✅ .NET 10 + ASP.NET Core 10 (with Aspire orchestration)
- ✅ PostgreSQL 17 with JSONB + GIN indexes
- ✅ Entity Framework Core 9.0
- ✅ SignalR 8.0+ (WebSocket)
- ✅ FluentValidation 11.9.2
- ✅ System.Threading.RateLimiting (built-in)

### Frontend
- ✅ React 18+ (TypeScript strict mode)
- ✅ Zustand 4.5+ (state management)
- ✅ TanStack Query 5.x (server state)
- ✅ React Router v7 (routing)
- ✅ Vite (build tool)
- ✅ Tailwind CSS (styling)

### DevOps
- ✅ Docker 25.x + Docker Compose 2.x
- ✅ GitHub Actions (CI/CD)
- ✅ Prometheus 2.45+ + Grafana 10+ (monitoring)
- ✅ Ubuntu 22.04 LTS (deployment OS)

---

## 🔐 25 Architectural Decisions (ALL LOCKED FOR MVP)

### Category 1: Data Architecture (5 decisions)
1. Hybrid EF Core + PostgreSQL JSONB ✓
2. EF Core Migrations with testing gate ✓
3. In-process BMAD agent router ✓
4. IMemoryCache (Phase 2: Redis) ✓
5. PostgreSQL GIN indexes for JSONB ✓

### Category 2: Authentication & Security (5 decisions)
6. Local DB auth (Phase 2: OpenID Connect) ✓
7. RBAC + Claims-based authorization ✓
8. HTTPS + TLS 1.3+ ✓
9. JWT (15-min) + HttpOnly Refresh (7-day) ✓
10. Per-user rate limiting (60 req/min) ✓

### Category 3: API & Communication (5 decisions)
11. Hybrid REST + RPC endpoints ✓
12. ProblemDetails RFC 7807 errors ✓
13. SignalR WebSocket (real-time) ✓
14. OpenAPI 3.1 + Swagger documentation ✓
15. URL versioning (/api/v1/) ✓

### Category 4: Frontend Architecture (5 decisions)
16. React 18 + TypeScript strict mode ✓
17. Zustand + TanStack Query ✓
18. React Router v7 with code splitting ✓
19. Tailwind CSS ✓
20. 120-150KB bundle size target ✓

### Category 5: Infrastructure & Deployment (5 decisions)
21. Aspire Docker Publisher (MVP) ✓
22. GitHub Actions + Docker Hub CI/CD ✓
23. .env + ConfigMaps configuration ✓
24. Prometheus + Grafana monitoring ✓
25. Progressive scaling (Docker → Swarm → K8s) ✓

---

## 📋 Critical Rules for All Development

### RULE 1: Version Control on All State Changes
```csharp
// ✅ REQUIRED
if (workflow.Version != expectedVersion)
    throw new WorkflowConflictException();
workflow.Version++;
await _context.SaveChangesAsync();

// ❌ FORBIDDEN
workflow.State = newState;
await _context.SaveChangesAsync();
```

### RULE 2: All Operations Are Async
```csharp
// ✅ REQUIRED
public async Task UpdateWorkflowAsync(...)

// ❌ FORBIDDEN
public void UpdateWorkflow(...)
```

### RULE 3: Use ProblemDetails for All Errors
```json
{
  "type": "https://bmadserver.api/errors/workflow-conflict",
  "title": "Workflow State Conflict",
  "status": 409,
  "detail": "Modified by another user",
  "expectedVersion": 5,
  "actualVersion": 6
}
```

### RULE 4: Authentication on ALL APIs
```csharp
app.MapPost("/api/v1/workflows", CreateWorkflow)
    .RequireAuthorization()  // Every endpoint!
    .WithOpenApi();
```

### RULE 5: Validate JSONB Before Persisting
```csharp
await validator.ValidateAndThrowAsync(request);
ValidateJsonbSchema(request);
workflow.State = request;
await _context.SaveChangesAsync();
```

---

## ⏱️ Timeline at a Glance

### Week 1 (Feb 3-7): Foundation
- Backend skeleton + auth MVP
- Database schema
- Frontend shell

### Week 2 (Feb 10-14): Core Features
- Workflow engine
- State management

### Week 3-4 (Feb 17 - Mar 2): Real-Time
- SignalR WebSocket
- Collaboration features

### Week 5-6 (Mar 3-16): Polish
- Testing
- Performance optimization
- Hardening

### Week 7-8 (Mar 17-30): Deployment
- Security validation
- Load testing
- Production deployment

---

## 📊 Performance Baselines (MUST MAINTAIN)

- **API:** 500 req/sec, <100ms p95 latency
- **WebSocket:** 100 concurrent connections, <50ms message latency
- **Database:** <20ms JSONB query with GIN index
- **Frontend:** 120-150KB bundle, <2s Time to Interactive
- **Code Splitting:** Per-route chunks 20-40KB each

---

## 🧪 Quality Gates (REQUIRED)

- **Unit Test Coverage:** ≥80%
- **Integration Tests:** All critical paths
- **E2E Tests:** Happy path + error scenarios
- **Load Test Baseline:** Verify 500 req/sec
- **Security Scan:** OWASP top 10 check

---

## 🚀 Getting Started (For New Developers)

1. **Read:** `developer-onboarding.md` (30 minutes)
2. **Setup:** Follow local development setup
3. **Explore:** Review `implementation-patterns.md` for code examples
4. **Reference:** Use `project-context-ai.md` for rules
5. **Code:** Follow patterns from ADRs

---

## ✅ Pre-Implementation Checklist

- [ ] All team members have read architecture.md
- [ ] Developer environment setup (prerequisites installed)
- [ ] GitHub repository cloned
- [ ] Docker Hub account configured
- [ ] GitHub Actions secrets configured
- [ ] Daily standups scheduled
- [ ] Aspire starter template cloned locally
- [ ] First build runs without errors

---

## 📞 When You Need Help

**Architecture Question?** → See `architecture.md` section 5 categories

**Code Example?** → See `implementation-patterns.md` or relevant ADR

**Setup Issue?** → See `developer-onboarding.md` troubleshooting

**Coding Rules?** → See `project-context-ai.md` critical rules

**Timeline Question?** → See `8-week-roadmap.md`

---

## 🎯 MVP Success Criteria

✅ Complete one BMAD workflow end-to-end  
✅ 2+ users collaborate without conflicts  
✅ State persists across refresh  
✅ 95% task completion rate  
✅ All P0 security met  
✅ API documented (Swagger)  
✅ Deployable to Linux  

---

## 🔗 Important Links

**Repository:** [Your GitHub URL]  
**Aspire Docs:** https://learn.microsoft.com/aspire  
**React Docs:** https://react.dev  
**PostgreSQL Docs:** https://www.postgresql.org/docs/17/  
**SignalR Docs:** https://learn.microsoft.com/aspnet/core/signalr  

---

**Ready to start Week 1? Begin with `developer-onboarding.md`!**

