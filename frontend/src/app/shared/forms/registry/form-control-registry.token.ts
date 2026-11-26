import { InjectionToken } from '@angular/core';
import { FormControlDefinition } from './form-control-definition.model';

export const FORM_CONTROL_DEFINITIONS = new InjectionToken<readonly FormControlDefinition[]>(
  'FORM_CONTROL_DEFINITIONS',
);
