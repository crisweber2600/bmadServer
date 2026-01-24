---
name: step-01-research
phase: analysis
model_preference: claude-sonnet-4
---

# Step 1: Research and Analysis

This step should trigger the step routing map rule:
- Workflow: complex-analysis-workflow
- Step: step-01-research
- Expected model: claude-sonnet-4 (per step routing configuration)

## Task

Analyze the requirements for implementing a data processing pipeline.

## Expected Behavior

The BMAD Router should:
1. Detect this as a BMAD step file via tool.execute.before hook
2. Extract workflow context: workflowId="complex-analysis-workflow", stepKey="step-01-research"  
3. Apply step routing map rule for "complex-analysis-workflow::step-01-research"
4. Select claude-sonnet-4 as specified in the step routing configuration
5. Log the decision to the trace file