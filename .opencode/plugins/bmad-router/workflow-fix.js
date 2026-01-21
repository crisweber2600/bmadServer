import { readFile } from 'fs/promises';
import { join } from 'path';
import * as yaml from 'js-yaml';

async function fixedDetectCurrentPhase(projectRoot) {
  const planningPath = join(projectRoot, '_bmad-output/planning-artifacts');
  const statusFile = join(planningPath, 'bmm-workflow-status.yaml');
  
  try {
    const content = await readFile(statusFile, 'utf-8');
    const status = yaml.load(content);
    
    // Check each phase for active workflows
    const phases = status.workflow_status;
    if (!phases) return 'unknown';
    
    // Phase 1 - Analysis: has complete workflow
    if (phases.phase_1_analysis?.workflows?.['product-brief']?.status === 'complete') {
      // Check if Phase 2 has any required workflows that are not complete
      const phase2 = phases.phase_2_planning?.workflows;
      if (phase2) {
        for (const [key, workflow] of Object.entries(phase2)) {
          if (workflow.status === 'required' && !workflow.completed_at) {
            return 'prd'; // or 'planning' 
          }
        }
      }
      
      // Check Phase 3 - Solutioning
      const phase3 = phases.phase_3_solutioning?.workflows;
      if (phase3) {
        for (const [key, workflow] of Object.entries(phase3)) {
          if (workflow.status === 'required' && !workflow.completed_at) {
            return 'architecture'; // or 'solutioning'
          }
        }
      }
      
      return 'planning'; // Default to planning phase since analysis is complete
    }
    
    return 'brainstorm'; // Default to brainstorm if analysis not complete
    
  } catch (err) {
    console.error('Error reading workflow status:', err.message);
    return 'unknown';
  }
}

// Test the function
fixedDetectCurrentPhase('/Users/cris/bmadServer').then(phase => {
  console.log('Fixed detection result:', phase);
});
