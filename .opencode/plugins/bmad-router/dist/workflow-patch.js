import { readFile } from 'fs/promises';
import { join } from 'path';
import * as yaml from 'js-yaml';
const PLANNING_ARTIFACTS_PATH = '_bmad-output/planning-artifacts';
const BMM_WORKFLOW_STATUS_FILE = 'bmm-workflow-status.yaml';
async function loadYaml(filePath) {
    try {
        const content = await readFile(filePath, 'utf-8');
        return yaml.load(content);
    }
    catch {
        return null;
    }
}
function getActivePhaseFromEnhancedWorkflowStatus(status) {
    if (status.current_phase === 'phase_2_planning') {
        const phase2 = status.workflow_status?.phase_2_planning;
        if (phase2?.workflows?.prd?.status === 'in_progress') {
            return 'prd';
        }
        if (phase2?.workflows?.['create-ux-design']?.status === 'in_progress') {
            return 'architecture';
        }
    }
    if (status.current_phase === 'phase_3_solutioning') {
        const phase3 = status.workflow_status?.phase_3_solutioning;
        if (phase3?.workflows?.['create-architecture']?.status === 'in_progress') {
            return 'architecture';
        }
    }
    if (status.current_phase === 'phase_4_implementation') {
        return 'quick-dev';
    }
    const workflows = status.workflow_status;
    if (!workflows)
        return 'unknown';
    const phase2 = workflows.phase_2_planning;
    if (phase2?.workflows) {
        if (phase2.workflows.prd?.status === 'in_progress' ||
            (phase2.workflows.prd?.status === 'required' && !phase2.workflows.prd?.completed_at)) {
            return 'prd';
        }
    }
    const phase3 = workflows.phase_3_solutioning;
    if (phase3?.workflows) {
        if (phase3.workflows['create-architecture']?.status === 'in_progress' ||
            (phase3.workflows['create-architecture']?.status === 'required' && !phase3.workflows['create-architecture']?.completed_at)) {
            return 'architecture';
        }
    }
    const phase1 = workflows.phase_1_analysis;
    if (phase1?.workflows) {
        if (phase1.workflows['product-brief']?.status !== 'complete') {
            return 'brainstorm';
        }
    }
    return 'unknown';
}
export async function detectCurrentPhaseEnhanced(projectRoot) {
    const planningPath = join(projectRoot, PLANNING_ARTIFACTS_PATH);
    const workflowStatus = await loadYaml(join(planningPath, BMM_WORKFLOW_STATUS_FILE));
    if (workflowStatus) {
        return getActivePhaseFromEnhancedWorkflowStatus(workflowStatus);
    }
    return 'unknown';
}
export async function testDetection() {
    const phase = await detectCurrentPhaseEnhanced('/Users/cris/bmadServer');
    console.log('Enhanced detection result:', phase);
    console.log('Expected: prd (since PRD is in_progress)');
}
