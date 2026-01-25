# Production Code Standards - Zero Placeholder Policy

**Status:** Active  
**Effective Date:** January 25, 2026 (Post-Epic 3 Review)  
**Last Updated:** January 25, 2026

---

## 🚨 THE RULE: NO PLACEHOLDER, DEMO, OR EXAMPLE CODE SHIPS TO USERS

**This is non-negotiable.** If code is deployed or shown to stakeholders/users, it must be production-ready.

---

## What This Means

### ✅ ALLOWED
- Real implementations that solve the problem
- Code that would function correctly for 5+ years without changes
- Temporary scaffolding that is REMOVED before shipping (not left as placeholder)
- Comments explaining complex logic
- TODOs with EXPLICIT Epic assignment for replacement

### ❌ NOT ALLOWED
- "Example" methods that demonstrate behavior but aren't real
- "Simulated" responses presented as if they were real features
- "Demo" code that works in one scenario only
- Stub implementations without replacement plan
- Methods named `Test*`, `Demo*`, `Example*`, `Temp*`, `Scaffold*`
- UI displaying placeholder text like "Simulated response" to users
- Code that returns fake/mocked data
- TODOs without an assigned Epic for completion

---

## What Happened in Epic 3

**Issue:** ChatHub.GenerateSimulatedResponse() shipped as placeholder code
- Method returned simulated responses instead of real workflow invocations
- UI displayed "Simulated response" text to stakeholders
- Confused expectations about feature maturity and readiness
- Appeared as unfinished/"example" code despite being in production

**Why It's Wrong:**
1. Users see "simulated" = they think feature is incomplete
2. Future developers see placeholder = uncertainty about relying on it
3. Code review discipline breaks = other placeholder code creeps in
4. Technical debt accumulates = "it's just temporary" → 3 years later it's still there

**Resolution:** Epic 4 MUST replace with real workflow orchestration calls

---

## Code Review Enforcement

### Reviewer Checklist

**BEFORE APPROVING ANY CODE, check for these anti-patterns:**

```
❌ Method/class named: *Example*, *Demo*, *Test*, *Temp*, *Simulated*, *Scaffold*
❌ Return value is: Fake data, mocked data, placeholder values, "Simulated" strings
❌ Comment says: "just for testing", "example implementation", "temporary", "demo only"
❌ TODO without: An Epic number assigned for completion (e.g., "TODO(Epic-4): ...")
❌ UI displays: "Example", "Demo", "Simulated", or any placeholder text to users
```

**If ANY red flag found:**
- 🛑 **REJECT** with reason: "No placeholder code in production. Implement real behavior or defer feature."
- 👉 **Point developer to:** This standard and Epic 3 retrospective

**If ALL checks pass:**
- ✅ **APPROVE** - This is production code

---

## Implementation Checklist

### Developer Responsibility
Before submitting code for review:
- [ ] Does this code meet the "5-year support test"?
- [ ] Would I explain this code to a customer and feel proud?
- [ ] Are there ANY placeholder/demo/example patterns?
- [ ] Does every TODO reference an Epic number?
- [ ] Would stakeholders feel confident using this feature?

### Code Review Responsibility
During review:
- [ ] Check for forbidden anti-patterns (see above)
- [ ] Verify method names don't suggest placeholder behavior
- [ ] Confirm return values are real, not simulated
- [ ] Validate all TODOs have Epic assignment
- [ ] Reject anything that looks like demo/example code

### Epic Completion Responsibility
Before marking epic "DONE":
- [ ] Grep source for: "example", "demo", "simulated", "temporary"
  ```bash
  grep -r "example\|demo\|simulated\|temporary" src/
  # Should return ONLY documentation or approved patterns
  ```
- [ ] Verify: No placeholder text visible to users
- [ ] Check: All replacement TODOs assigned to next Epic
- [ ] Confirm: Every method is production-ready as-is

---

## Approved Patterns for Temporary Code

### IF You Must Use Temporary Code:

1. **Use explicit naming convention:**
   ```csharp
   // CORRECT: Explicitly marks as temporary
   private string TEMP_GenerateSimulatedResponse(string userMessage)
   {
       // TODO(Epic-4): Replace with real workflow orchestration
   }
   
   // WRONG: Hides the fact that it's temporary
   private string GenerateSimulatedResponse(string userMessage)
   ```

2. **Code is NEVER shipped to users:**
   - Only used internally for development/testing
   - Removed before feature ships
   - Not called from production code paths

3. **Explicit comment required:**
   ```csharp
   // TEMPORARY: This method returns fake responses.
   // Must be replaced by Epic-4 Story 4-2: Workflow Invocation
   // Do not use for production queries.
   private string TEMP_MockWorkflowResponse() { ... }
   ```

4. **Assigned to next Epic's critical path:**
   - Epic 4's acceptance criteria includes: "Remove all TEMP_* methods"
   - Epic 4 stories explicitly replace temporary code

---

## Examples of What We DON'T DO Again

### Example 1: The GenerateSimulatedResponse Problem (Epic 3)
```csharp
// ❌ WRONG
private string GenerateSimulatedResponse(string userMessage)
{
    return "Simulated response: " + userMessage;  // Fake data
}
```

**Why it's wrong:**
- Name doesn't signal it's temporary
- Returns fake data
- UI shows "Simulated response" to users
- Confuses stakeholders

**What we do instead:**
- Either: Implement REAL workflow invocation (Epic 4 work)
- Or: Defer this feature until Epic 4 is ready

### Example 2: The Demo Method
```csharp
// ❌ WRONG
public class UserService
{
    public ExampleUserDemoForTesting() { ... }  // Name signals it's not real
    
    public List<User> GetDemoUsers() { ... }  // Returns fake data
}
```

**Why it's wrong:**
- Method names explicitly signal they're demo/example
- Shipped to production
- Future developers get confused

**What we do instead:**
- Remove demo methods entirely before shipping
- Use test fixtures in test code only

---

## Questions to Ask During Code Review

1. **"Would we support this for 5 years?"**
   - If NO → Don't ship it yet. Work more or defer to next Epic.

2. **"Is this method name transparent about its purpose?"**
   - If it sounds like "demo/example/test" → Reject it.

3. **"If stakeholders saw this code, would they feel confident?"**
   - If NO → It's not ready to ship.

4. **"Does every TODO point to a specific Epic for completion?"**
   - If NO → Add the Epic number or reject.

5. **"Is any data returned to users fake or simulated?"**
   - If YES → Reject it. Real data only.

---

## References

- **Epic 3 Retrospective:** Documents the GenerateSimulatedResponse() issue
- **Pattern 3 Analysis:** Explains why placeholder code is problematic
- **Code Review Checklist:** Enforcement tools in retrospective document

---

## Enforcement Timeline

| When | Action |
|------|--------|
| **Now (Epic 4+)** | Code review rejects all placeholder patterns |
| **Pre-Epic Completion** | Grep verification removes all demo/example/simulated code |
| **Code Review Meeting** | Discuss any edge cases; maintain zero-tolerance policy |

---

**Remember:** 
- Production code must be PRODUCTION READY
- Placeholder code is a communication failure
- "We'll fix it later" always turns into "we never did"
- Stakeholders deserve to see finished features, not demos

This standard starts now and continues forever.
