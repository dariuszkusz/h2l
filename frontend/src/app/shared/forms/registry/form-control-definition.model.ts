import { components } from '../../../core/api/generated/openapi.types';
import { FormControlType } from '../utils/form-control-type.type';
import { FormControlCategory } from './form-control-category.type';

type FormComponent = components['schemas']['FormComponent'];

export interface FormControlDefinition {
  readonly type: FormControlType;
  readonly category: FormControlCategory;
  readonly labelKey: string;
  readonly collectsData: boolean;
  readonly supportsOptions: boolean;
  readonly supportsChildren: boolean;
  readonly defaultValue: unknown;
  readonly createDefaultComponent: (index: number) => FormComponent;
}
