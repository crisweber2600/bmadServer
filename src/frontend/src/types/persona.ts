/**
 * Persona Types
 * Matches backend PersonaType enum
 */

/** Persona type enum - values match backend exactly */
export const PersonaType = {
  Business: 0,
  Technical: 1,
  Hybrid: 2,
} as const;

export type PersonaType = typeof PersonaType[keyof typeof PersonaType];

/** Human-readable labels for personas */
export const PersonaLabels: Record<PersonaType, string> = {
  [PersonaType.Business]: 'Business',
  [PersonaType.Technical]: 'Technical',
  [PersonaType.Hybrid]: 'Hybrid',
};

/** Descriptions for personas */
export const PersonaDescriptions: Record<PersonaType, string> = {
  [PersonaType.Business]: 'Non-technical explanations with business context',
  [PersonaType.Technical]: 'Detailed technical information with implementation details',
  [PersonaType.Hybrid]: 'Balanced view with both business and technical perspectives',
};

/** Request body for PUT /api/v1/sessions/{id}/persona */
export interface PersonaSwitchRequest {
  PersonaType: PersonaType;
}

/** Response from PUT /api/v1/sessions/{id}/persona */
export interface PersonaSwitchResponse {
  Success: boolean;
  NewPersona: PersonaType;
  PreviousPersona: PersonaType;
}

/** Glossary term for persona-specific terminology */
export interface GlossaryTerm {
  term: string;
  definition: string;
  category?: 'business' | 'technical' | 'general';
  relatedTerms?: string[];
}
