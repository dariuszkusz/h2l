import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { components } from '../../../core/api/generated/openapi.types';
import { TranslationService } from '../../../core/i18n/services/translation.service';
import { TranslatePipe } from '../../pipes/translate.pipe';
import { HlmBadgeDirective } from '../../ui/hlm-badge.directive';
import { HlmButtonDirective } from '../../ui/hlm-button.directive';
import { HlmCardDirective } from '../../ui/hlm-card.directive';
import { HlmInputDirective } from '../../ui/hlm-input.directive';
import { HlmLabelDirective } from '../../ui/hlm-label.directive';
import { evaluateBooleanExpression, evaluateExpression } from '../ast/safe-ast-evaluator';
import { resolveLocalizedText } from '../utils/resolve-localized-text';
import { FormFileUploadEvent } from './form-file-upload-event.model';

type FormSchema = components['schemas']['FormSchema'];
type FormComponent = components['schemas']['FormComponent'];
type FormFieldValidator = components['schemas']['FormFieldValidator'];
type LocalizedText = components['schemas']['LocalizedText'];
type SupportedLanguage = components['schemas']['SupportedLanguage'];
type AttachmentUploadResponse = components['schemas']['AttachmentUploadResponse'];

const NON_DATA_TYPES: ReadonlySet<string> = new Set([
  'section',
  'grid',
  'heading',
  'text_block',
  'divider',
]);

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

@Component({
  selector: 'app-form-renderer',
  standalone: true,
  imports: [
    NgTemplateOutlet,
    ReactiveFormsModule,
    TranslatePipe,
    HlmButtonDirective,
    HlmCardDirective,
    HlmBadgeDirective,
    HlmInputDirective,
    HlmLabelDirective,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './form-renderer.component.html',
})
export class FormRendererComponent {
  private readonly translationService = inject(TranslationService);

  readonly schema = input.required<FormSchema>();
  readonly language = input<SupportedLanguage>('en');
  readonly prefilledData = input<Readonly<Record<string, unknown>>>({});
  readonly initialDraftValues = input<Readonly<Record<string, unknown>> | null>(null);
  readonly readOnlyMode = input<boolean>(false);
  readonly submitting = input<boolean>(false);
  readonly uploadedAttachments = input<readonly AttachmentUploadResponse[]>([]);
  readonly submitLabelKey = input<string>('formRenderer.submit');

  readonly valuesChanged = output<Readonly<Record<string, unknown>>>();
  readonly formSubmitted = output<Readonly<Record<string, unknown>>>();
  readonly fileSelected = output<FormFileUploadEvent>();

  protected readonly formGroup = signal<FormGroup>(new FormGroup({}));
  protected readonly currentValues = signal<Readonly<Record<string, unknown>>>({});
  protected readonly currentStepIndex = signal<number>(0);
  protected readonly fieldErrors = signal<Readonly<Record<string, string>>>({});
  protected readonly fileErrors = signal<Readonly<Record<string, string>>>({});
  protected readonly announcement = signal<string>('');
  protected readonly repeaterRows = signal<Readonly<Record<string, readonly Record<string, unknown>[]>>>({});

  protected readonly isWizard = computed(() => this.schema().layoutMode === 'wizard');
  protected readonly steps = computed(() => this.schema().steps ?? []);
  protected readonly activeStep = computed(() => {
    const allSteps = this.steps();
    const idx = Math.min(this.currentStepIndex(), Math.max(allSteps.length - 1, 0));
    return allSteps[idx] ?? null;
  });

  protected readonly isFirstStep = computed(() => this.currentStepIndex() <= 0);
  protected readonly isLastStep = computed(
    () => this.currentStepIndex() >= Math.max(this.steps().length - 1, 0),
  );

  private valueSub: { unsubscribe(): void } | null = null;

  constructor() {
    effect((onCleanup) => {
      const currentSchema = this.schema();
      const prefilled = this.prefilledData();
      const draft = this.initialDraftValues();
      const builtGroup = this.buildFormGroup(currentSchema, prefilled, draft);

      this.valueSub?.unsubscribe();
      this.formGroup.set(builtGroup);
      this.currentStepIndex.set(0);
      this.fieldErrors.set({});
      this.fileErrors.set({});
      this.currentValues.set(this.collectValuesSnapshot(builtGroup));

      this.valueSub = builtGroup.valueChanges.subscribe(() => {
        const snapshot = this.collectValuesSnapshot(builtGroup);
        this.currentValues.set(snapshot);
        this.valuesChanged.emit(snapshot);
      });

      onCleanup(() => {
        this.valueSub?.unsubscribe();
        this.valueSub = null;
      });
    });
  }

  protected localized(text: LocalizedText | undefined | null): string {
    return resolveLocalizedText(text, this.language(), this.schema().defaultLanguage ?? 'en');
  }

  protected isVisible(component: FormComponent): boolean {
    return evaluateBooleanExpression(component.rules?.visibleIf, this.currentValues(), true);
  }

  protected isRequired(component: FormComponent): boolean {
    const hasStaticRequired = (component.validators ?? []).some((v) => v.type === 'required');
    if (hasStaticRequired) {
      return true;
    }
    return evaluateBooleanExpression(component.rules?.requiredIf, this.currentValues(), false);
  }

  protected isReadOnly(component: FormComponent): boolean {
    if (this.readOnlyMode() || component.readOnly === true) {
      return true;
    }
    return evaluateBooleanExpression(component.rules?.readOnlyIf, this.currentValues(), false);
  }

  protected fieldId(component: FormComponent): string {
    return `h2l-field-${component.id}`;
  }

  protected errorId(component: FormComponent): string {
    return `h2l-err-${component.id}`;
  }

  protected hintId(component: FormComponent): string {
    return `h2l-hint-${component.id}`;
  }

  protected describedBy(component: FormComponent): string | null {
    const ids: string[] = [];
    if (this.localized(component.description).length > 0) {
      ids.push(this.hintId(component));
    }
    if (this.getFieldError(component) !== null) {
      ids.push(this.errorId(component));
    }
    return ids.length > 0 ? ids.join(' ') : null;
  }

  protected getFieldError(component: FormComponent): string | null {
    const key = component.variableKey;
    if (!key) {
      return null;
    }
    return this.fileErrors()[key] ?? this.fieldErrors()[key] ?? null;
  }

  protected isCheckboxGroupChecked(variableKey: string | undefined, optionValue: string): boolean {
    if (!variableKey) {
      return false;
    }
    const raw = this.formGroup().get(variableKey)?.value;
    return Array.isArray(raw) && raw.includes(optionValue);
  }

  protected onCheckboxGroupToggle(
    component: FormComponent,
    optionValue: string,
    event: Event,
  ): void {
    const key = component.variableKey;
    if (!key || this.isReadOnly(component)) {
      return;
    }
    const checkbox = event.target as HTMLInputElement;
    const control = this.formGroup().get(key);
    if (!control) {
      return;
    }
    const existing = Array.isArray(control.value) ? [...(control.value as string[])] : [];
    const updated = checkbox.checked
      ? existing.includes(optionValue)
        ? existing
        : [...existing, optionValue]
      : existing.filter((v) => v !== optionValue);
    control.setValue(updated);
    control.markAsTouched();
  }

  protected onToggleChange(component: FormComponent): void {
    const key = component.variableKey;
    if (!key || this.isReadOnly(component)) {
      return;
    }
    const control = this.formGroup().get(key);
    if (!control) {
      return;
    }
    control.setValue(!Boolean(control.value));
    control.markAsTouched();
  }

  protected onFileInputChange(component: FormComponent, event: Event): void {
    const key = component.variableKey;
    if (!key || this.isReadOnly(component)) {
      return;
    }
    const inputEl = event.target as HTMLInputElement;
    const file = inputEl.files?.[0];
    if (!file) {
      return;
    }

    const allowedTypes = component.fileConfig?.allowedMimeTypes ?? [];
    if (allowedTypes.length > 0 && !allowedTypes.includes(file.type)) {
      this.fileErrors.update((prev) => ({
        ...prev,
        [key]: this.translationService.translate('formRenderer.errors.invalidFileType', this.language()),
      }));
      return;
    }

    const maxBytes = component.fileConfig?.maxSizeBytes ?? 10 * 1024 * 1024;
    if (file.size > maxBytes) {
      this.fileErrors.update((prev) => ({
        ...prev,
        [key]: this.translationService.translate('formRenderer.errors.fileTooLarge', this.language(), {
          maxMb: Math.round(maxBytes / (1024 * 1024)),
        }),
      }));
      return;
    }

    this.fileErrors.update((prev) => {
      const next = { ...prev };
      delete next[key];
      return next;
    });

    const control = this.formGroup().get(key);
    if (control) {
      control.setValue(file.name);
      control.markAsTouched();
    }

    this.fileSelected.emit({ fieldVariableKey: key, file });
  }

  protected attachmentsForField(variableKey: string | undefined): readonly AttachmentUploadResponse[] {
    if (!variableKey) {
      return [];
    }
    return this.uploadedAttachments().filter((a) => a.fieldVariableKey === variableKey);
  }

  protected getRepeaterRows(component: FormComponent): readonly Record<string, unknown>[] {
    const key = component.variableKey;
    if (!key) {
      return [];
    }
    return this.repeaterRows()[key] ?? [];
  }

  protected onAddRepeaterRow(component: FormComponent): void {
    const key = component.variableKey;
    if (!key || this.isReadOnly(component)) {
      return;
    }
    const newRow: Record<string, unknown> = {};
    for (const child of component.components ?? []) {
      if (child.variableKey) {
        newRow[child.variableKey] = '';
      }
    }
    const updated = [...this.getRepeaterRows(component), newRow];
    this.syncRepeaterRows(key, updated);
  }

  protected onRemoveRepeaterRow(component: FormComponent, rowIndex: number): void {
    const key = component.variableKey;
    if (!key || this.isReadOnly(component)) {
      return;
    }
    const updated = this.getRepeaterRows(component).filter((_, idx) => idx !== rowIndex);
    this.syncRepeaterRows(key, updated);
  }

  protected onRepeaterCellInput(
    component: FormComponent,
    rowIndex: number,
    childKey: string | undefined,
    event: Event,
  ): void {
    const key = component.variableKey;
    if (!key || !childKey || this.isReadOnly(component)) {
      return;
    }
    const value = (event.target as HTMLInputElement).value;
    const updated = this.getRepeaterRows(component).map((row, idx) =>
      idx === rowIndex ? { ...row, [childKey]: value } : row,
    );
    this.syncRepeaterRows(key, updated);
  }

  protected gridColumnsClass(component: FormComponent): string {
    const cols = component.columns ?? 2;
    if (cols >= 3) {
      return 'grid gap-4 sm:grid-cols-3';
    }
    if (cols === 1) {
      return 'grid gap-4 grid-cols-1';
    }
    return 'grid gap-4 sm:grid-cols-2';
  }

  protected onPreviousStep(): void {
    if (!this.isFirstStep()) {
      this.currentStepIndex.update((i) => i - 1);
      this.announceStepChange();
    }
  }

  protected onNextStep(): void {
    const step = this.activeStep();
    if (!step) {
      return;
    }
    const errors = this.validateComponents(step.components);
    this.fieldErrors.set(errors);
    if (Object.keys(errors).length > 0) {
      this.announcement.set(
        this.translationService.translate('formRenderer.errors.fixStepErrors', this.language()),
      );
      return;
    }
    if (!this.isLastStep()) {
      this.currentStepIndex.update((i) => i + 1);
      this.announceStepChange();
    }
  }

  protected onSubmit(): void {
    if (this.readOnlyMode()) {
      return;
    }
    const allComponents = this.steps().flatMap((s) => s.components);
    const errors = this.validateComponents(allComponents);
    this.fieldErrors.set(errors);

    if (Object.keys(errors).length > 0 || Object.keys(this.fileErrors()).length > 0) {
      this.announcement.set(
        this.translationService.translate('formRenderer.errors.fixFormErrors', this.language()),
      );
      return;
    }

    const payload = this.collectVisiblePayload();
    this.formSubmitted.emit(payload);
  }

  private syncRepeaterRows(key: string, rows: readonly Record<string, unknown>[]): void {
    this.repeaterRows.update((prev) => ({ ...prev, [key]: rows }));
    const control = this.formGroup().get(key);
    if (control) {
      control.setValue(rows);
    }
  }

  private announceStepChange(): void {
    const step = this.activeStep();
    if (!step) {
      return;
    }
    this.announcement.set(
      this.translationService.translate('formRenderer.stepAnnounce', this.language(), {
        current: this.currentStepIndex() + 1,
        total: this.steps().length,
        title: this.localized(step.title),
      }),
    );
  }

  private buildFormGroup(
    schema: FormSchema,
    prefilled: Readonly<Record<string, unknown>>,
    draft: Readonly<Record<string, unknown>> | null,
  ): FormGroup {
    const controls: Record<string, FormControl<unknown>> = {};
    const initialRepeaters: Record<string, readonly Record<string, unknown>[]> = {};

    const registerComponent = (component: FormComponent): void => {
      const compType = component.type as string;
      if (!NON_DATA_TYPES.has(compType) && component.variableKey) {
        const key = component.variableKey;
        const hasPrefilled = Object.prototype.hasOwnProperty.call(prefilled, key);
        const hasDraft = draft !== null && Object.prototype.hasOwnProperty.call(draft, key);

        let initialVal: unknown = component.defaultValue ?? this.defaultValueForType(compType);
        if (hasPrefilled) {
          initialVal = prefilled[key];
        }
        if (hasDraft && !component.readOnly) {
          initialVal = draft[key];
        }

        if (compType === 'repeater') {
          const rows = Array.isArray(initialVal)
            ? (initialVal as Record<string, unknown>[])
            : [];
          initialRepeaters[key] = rows;
          controls[key] = new FormControl<unknown>(rows);
        } else {
          controls[key] = new FormControl<unknown>(initialVal);
        }
      }

      if (compType !== 'repeater' && component.components) {
        for (const child of component.components) {
          registerComponent(child);
        }
      }
    };

    for (const step of schema.steps ?? []) {
      for (const component of step.components) {
        registerComponent(component);
      }
    }

    this.repeaterRows.set(initialRepeaters);
    return new FormGroup(controls);
  }

  private defaultValueForType(type: string): unknown {
    switch (type) {
      case 'checkbox':
      case 'toggle':
        return false;
      case 'checkbox_group':
      case 'repeater':
        return [];
      case 'number':
      case 'file':
        return null;
      default:
        return '';
    }
  }

  private collectValuesSnapshot(group: FormGroup): Readonly<Record<string, unknown>> {
    const raw = group.getRawValue() as Record<string, unknown>;
    const normalized: Record<string, unknown> = {};
    for (const [key, value] of Object.entries(raw)) {
      normalized[key] = value;
    }
    return normalized;
  }

  private collectVisiblePayload(): Readonly<Record<string, unknown>> {
    const allValues = this.currentValues();
    const result: Record<string, unknown> = {};

    const collect = (componentsList: readonly FormComponent[]): void => {
      for (const comp of componentsList) {
        if (!this.isVisible(comp)) {
          continue;
        }
        const compType = comp.type as string;
        if (!NON_DATA_TYPES.has(compType) && comp.variableKey) {
          result[comp.variableKey] = allValues[comp.variableKey];
        }
        if (compType !== 'repeater' && comp.components) {
          collect(comp.components);
        }
      }
    };

    for (const step of this.steps()) {
      collect(step.components);
    }

    return result;
  }

  private validateComponents(
    componentsList: readonly FormComponent[],
  ): Readonly<Record<string, string>> {
    const errors: Record<string, string> = {};
    const values = this.currentValues();

    const visit = (items: readonly FormComponent[]): void => {
      for (const comp of items) {
        if (!this.isVisible(comp)) {
          continue;
        }
        const compType = comp.type as string;
        if (!NON_DATA_TYPES.has(compType) && comp.variableKey) {
          const errorMsg = this.validateSingleComponent(comp, values);
          if (errorMsg) {
            errors[comp.variableKey] = errorMsg;
          }
        }
        if (compType !== 'repeater' && comp.components) {
          visit(comp.components);
        }
      }
    };

    visit(componentsList);
    return errors;
  }

  private validateSingleComponent(
    component: FormComponent,
    values: Readonly<Record<string, unknown>>,
  ): string | null {
    const key = component.variableKey;
    if (!key) {
      return null;
    }

    const value = values[key];
    const isEmpty =
      value === null ||
      value === undefined ||
      (typeof value === 'string' && value.trim().length === 0) ||
      (Array.isArray(value) && value.length === 0) ||
      ((component.type === 'checkbox' || component.type === 'toggle') && value === false);

    if (this.isRequired(component) && isEmpty) {
      const reqValidator = (component.validators ?? []).find((v) => v.type === 'required');
      const customMsg = this.localized(reqValidator?.message);
      return customMsg || this.translationService.translate('common.validation.required', this.language());
    }

    if (isEmpty) {
      return null;
    }

    for (const validator of component.validators ?? []) {
      const error = this.runValidator(validator, value, values);
      if (error) {
        return error;
      }
    }

    return null;
  }

  private runValidator(
    validator: FormFieldValidator,
    value: unknown,
    allValues: Readonly<Record<string, unknown>>,
  ): string | null {
    const custom = this.localized(validator.message);

    switch (validator.type) {
      case 'required':
        return null;

      case 'min': {
        const min = Number(validator.value);
        if (!Number.isNaN(min) && Number(value) < min) {
          return (
            custom ||
            this.translationService.translate('formRenderer.errors.min', this.language(), { min })
          );
        }
        return null;
      }

      case 'max': {
        const max = Number(validator.value);
        if (!Number.isNaN(max) && Number(value) > max) {
          return (
            custom ||
            this.translationService.translate('formRenderer.errors.max', this.language(), { max })
          );
        }
        return null;
      }

      case 'minLength': {
        const min = Number(validator.value);
        if (!Number.isNaN(min) && String(value).length < min) {
          return (
            custom ||
            this.translationService.translate('common.validation.minLength', this.language(), { min })
          );
        }
        return null;
      }

      case 'maxLength': {
        const max = Number(validator.value);
        if (!Number.isNaN(max) && String(value).length > max) {
          return (
            custom ||
            this.translationService.translate('common.validation.maxLength', this.language(), { max })
          );
        }
        return null;
      }

      case 'email': {
        if (!EMAIL_PATTERN.test(String(value))) {
          return (
            custom ||
            this.translationService.translate('common.validation.invalidEmail', this.language())
          );
        }
        return null;
      }

      case 'regex': {
        if (validator.pattern) {
          try {
            const rx = new RegExp(validator.pattern);
            if (!rx.test(String(value))) {
              return (
                custom ||
                this.translationService.translate('formRenderer.errors.pattern', this.language())
              );
            }
          } catch {
            return null;
          }
        }
        return null;
      }

      case 'compareField': {
        if (validator.targetField && validator.operator) {
          const targetVal = allValues[validator.targetField];
          if (targetVal !== undefined && targetVal !== null && targetVal !== '') {
            const passed = this.compareValues(value, targetVal, validator.operator);
            if (!passed) {
              return (
                custom ||
                this.translationService.translate('formRenderer.errors.compareField', this.language())
              );
            }
          }
        }
        return null;
      }

      case 'expression': {
        if (validator.expression) {
          try {
            const result = Boolean(evaluateExpression(validator.expression, allValues));
            if (!result) {
              return (
                custom ||
                this.translationService.translate('formRenderer.errors.expression', this.language())
              );
            }
          } catch {
            return (
              custom ||
              this.translationService.translate('formRenderer.errors.expression', this.language())
            );
          }
        }
        return null;
      }
    }
  }

  private compareValues(
    left: unknown,
    right: unknown,
    op: NonNullable<FormFieldValidator['operator']>,
  ): boolean {
    const a = typeof left === 'number' || !Number.isNaN(Number(left)) ? Number(left) : String(left);
    const b = typeof right === 'number' || !Number.isNaN(Number(right)) ? Number(right) : String(right);
    switch (op) {
      case 'eq':
        return a === b;
      case 'neq':
        return a !== b;
      case 'gt':
        return a > b;
      case 'gte':
        return a >= b;
      case 'lt':
        return a < b;
      case 'lte':
        return a <= b;
    }
  }
}
