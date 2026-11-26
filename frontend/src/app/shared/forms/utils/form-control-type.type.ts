import { components } from '../../../core/api/generated/openapi.types';

export type OpenApiFormComponentType = components['schemas']['FormComponentType'];

export type FormControlType = OpenApiFormComponentType | 'checkbox_group' | 'text_block';
