import { readFile } from 'fs/promises';
import { join } from 'path';
import * as yaml from 'js-yaml';
import type { BmadPhase, BmmWorkflowStatus, SprintStatus } from './types';

const PLANNING_ARTIFACTS_PATH = '_bmad-output/planning-artifacts';
const BMM_WORKFLOW_STATUS_FILE = 'bmm-workflow-status.yaml';
const SPRINT_STATUS_FILE = 'sprint-status.yaml';

async function loadYaml<T>(filePath: string): Promise<T | null> {
  try {
    const content = await readFile(filePath, 'utf-8');
    return yaml.load(content) as T;
  } catch {
    return null;
  }
}

function getActivePhaseFromWorkflowStatus(status: BmmWorkflowStatus): BmadPhase | null {
  const workflowStatus = status.workflow_status;
  if (!workflowStatus) return null;

  const phases: BmadPhase[] = ['quick-spec', 'quick-dev', 'code-review'];
  for (const phase of phases) {
    if (workflowStatus[phase] === 'in-progress') {
      return phase;
    }
  }
  return null;
}

function getActivePhaseFromSprintStatus(status: SprintStatus): BmadPhase | null {
  const devStatus = status.development_status;
  if (!devStatus) return null;

  for (const [key, value] of Object.entries(devStatus)) {
    if (key.match(/^\d+-\d+-/) && (value === 'in-progress' || value === 'review')) {
      return value as BmadPhase;
    }
  }
  return null;
}

export async function detectCurrentPhase(projectRoot: string): Promise<BmadPhase> {
  const planningPath = join(projectRoot, PLANNING_ARTIFACTS_PATH);

  const sprintStatus = await loadYaml<SprintStatus>(join(planningPath, SPRINT_STATUS_FILE));
  if (sprintStatus) {
    const sprintPhase = getActivePhaseFromSprintStatus(sprintStatus);
    if (sprintPhase) return sprintPhase;
  }

  const workflowStatus = await loadYaml<BmmWorkflowStatus>(join(planningPath, BMM_WORKFLOW_STATUS_FILE));
  if (workflowStatus) {
    const workflowPhase = getActivePhaseFromWorkflowStatus(workflowStatus);
    if (workflowPhase) return workflowPhase;
  }

  return 'unknown';
}
