import { CdkDrag, CdkDragDrop, CdkDropList, moveItemInArray } from '@angular/cdk/drag-drop';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnDestroy,
  OnInit,
  signal,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { components } from '../../../core/api/generated/openapi.types';
import { SupportedLanguage } from '../../../core/i18n/models/supported-language.type';
import { TranslationService } from '../../../core/i18n/services/translation.service';
import { selectCurrentLanguage } from '../../../core/i18n/store/i18n.selectors';
import { UiStateBannerComponent } from '../../../shared/components/ui-state-banner/ui-state-banner.component';
import { WorkflowTemplateExportComponent } from '../../../shared/components/workflow-template-export/workflow-template-export.component';
import { validateExpressionSyntax } from '../../../shared/forms/ast/safe-ast-evaluator';
import { FormRendererComponent } from '../../../shared/forms/form-renderer/form-renderer.component';
import { FormControlCategory } from '../../../shared/forms/registry/form-control-category.type';
import { FormControlRegistryService } from '../../../shared/forms/registry/form-control-registry.service';
import { FormControlType } from '../../../shared/forms/utils/form-control-type.type';
import {
  isValidVariableKey,
  validateFormSchema,
} from '../../../shared/forms/utils/form-schema-validator';
import { resolveLocalizedText } from '../../../shared/forms/utils/resolve-localized-text';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { HlmBadgeDirective } from '../../../shared/ui/hlm-badge.directive';
import { HlmButtonDirective } from '../../../shared/ui/hlm-button.directive';
import { HlmCardDirective } from '../../../shared/ui/hlm-card.directive';
import { HlmInputDirective } from '../../../shared/ui/hlm-input.directive';
import { HlmLabelDirective } from '../../../shared/ui/hlm-label.directive';
import { FormBuilderActions } from '../store/form-builder.actions';
import {
  selectDraftName,
  selectDraftSchema,
  selectFormBuilderDirty,
  selectFormBuilderError,
  selectFormBuilderLoading,
  selectFormBuilderPublishing,
  selectFormBuilderSaving,
  selectFormBuilderSuccessKey,
  selectFormDefinition,
  selectFormVersions,
  selectFormVersionsLoading,
  selectSelectedComponentId,
  selectSelectedStepIndex,
} from '../store/form-builder.selectors';

type FormSchema = components['schemas']['FormSchema'];
type FormComponent = components['schemas']['FormComponent'];
type FormFieldValidator = components['schemas']['FormFieldValidator'];
type FormValidatorType = components['schemas']['FormValidatorType'];
type FormLayoutMode = components['schemas']['FormLayoutMode'];

@Component({
  selector: 'app-form-builder-page',
  standalone: true,
  imports: [
    RouterLink,
    CdkDropList,
    CdkDrag,
    TranslatePipe,
    HlmButtonDirective,
    HlmCardDirective,
    HlmBadgeDirective,
    HlmInputDirective,
    HlmLabelDirective,
    UiStateBannerComponent,
    WorkflowTemplateExportComponent,
    FormRendererComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './form-builder-page.component.html',
})
export class FormBuilderPageComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly store = inject(Store);
  private readonly registry = inject(FormControlRegistryService);
  private readonly translationService = inject(TranslationService);

  protected readonly currentLanguage = this.store.selectSignal(selectCurrentLanguage);
  protected readonly formDefinition = this.store.selectSignal(selectFormDefinition);
  protected readonly draftName = this.store.selectSignal(selectDraftName);
  protected readonly draftSchema = this.store.selectSignal(selectDraftSchema);
  protected readonly versions = this.store.selectSignal(selectFormVersions);
  protected readonly selectedStepIndex = this.store.selectSignal(selectSelectedStepIndex);
  protected readonly selectedComponentId = this.store.selectSignal(selectSelectedComponentId);
  protected readonly dirty = this.store.selectSignal(selectFormBuilderDirty);
  protected readonly loading = this.store.selectSignal(selectFormBuilderLoading);
  protected readonly loadingVersions = this.store.selectSignal(selectFormVersionsLoading);
  protected readonly saving = this.store.selectSignal(selectFormBuilderSaving);
  protected readonly publishing = this.store.selectSignal(selectFormBuilderPublishing);
  protected readonly error = this.store.selectSignal(selectFormBuilderError);
  protected readonly successKey = this.store.selectSignal(selectFormBuilderSuccessKey);

  protected readonly activeTab = signal<'design' | 'preview' | 'versions'>('design');
  protected readonly previewViewport = signal<'desktop' | 'mobile'>('desktop');
  protected readonly previewLanguage = signal<SupportedLanguage>('en');
  protected readonly inspectorLanguage = signal<SupportedLanguage>('en');
  protected readonly liveAnnouncement = signal<string>('');
  protected readonly previewedVersionSchema = signal<FormSchema | null>(null);

  protected readonly categories: readonly FormControlCategory[] = [
    'data',
    'choice',
    'layout',
    'advanced',
  ];
  protected readonly languages: readonly SupportedLanguage[] = ['en', 'de', 'pl'];
  protected readonly clickableValidatorTypes: readonly FormValidatorType[] = [
    'required',
    'min',
    'max',
    'minLength',
    'maxLength',
    'email',
    'regex',
    'compareField',
  ];

  protected readonly formId = signal<string>('');
  private componentCounter = 1;

  protected readonly activeStep = computed(() => {
    const schema = this.draftSchema();
    if (!schema || schema.steps.length === 0) {
      return null;
    }
    const idx = Math.min(this.selectedStepIndex(), schema.steps.length - 1);
    return schema.steps[idx] ?? null;
  });

  protected readonly selectedComponent = computed<FormComponent | null>(() => {
    const step = this.activeStep();
    const id = this.selectedComponentId();
    if (!step || !id) {
      return null;
    }
    return step.components.find((c) => c.id === id) ?? null;
  });

  protected readonly schemaIssues = computed(() => {
    const schema = this.draftSchema();
    return schema ? validateFormSchema(schema) : [];
  });

  protected readonly selectedVariableKeyError = computed<string | null>(() => {
    const comp = this.selectedComponent();
    if (!comp) {
      return null;
    }
    const def = this.registry.get(comp.type);
    if (def && !def.collectsData) {
      return null;
    }
    const key = comp.variableKey?.trim() ?? '';
    if (key.length === 0) {
      return 'formBuilder.inspector.variableKeyRequired';
    }
    if (!isValidVariableKey(key)) {
      return 'formBuilder.inspector.variableKeyInvalid';
    }
    const schema = this.draftSchema();
    if (schema) {
      const count = schema.steps
        .flatMap((s) => s.components)
        .filter((c) => c.variableKey === key).length;
      if (count > 1) {
        return 'formBuilder.inspector.variableKeyDuplicate';
      }
    }
    return null;
  });

  protected readonly selectedExpressionErrors = computed(() => {
    const comp = this.selectedComponent();
    if (!comp) {
      return { visibleIf: null, requiredIf: null, readOnlyIf: null, expression: null };
    }
    const exprValidator = (comp.validators ?? []).find((v) => v.type === 'expression');
    return {
      visibleIf: comp.rules?.visibleIf ? validateExpressionSyntax(comp.rules.visibleIf) : null,
      requiredIf: comp.rules?.requiredIf ? validateExpressionSyntax(comp.rules.requiredIf) : null,
      readOnlyIf: comp.rules?.readOnlyIf ? validateExpressionSyntax(comp.rules.readOnlyIf) : null,
      expression: exprValidator?.expression
        ? validateExpressionSyntax(exprValidator.expression)
        : null,
    };
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('formId') ?? '';
    this.formId.set(id);
    this.previewLanguage.set(this.currentLanguage());
    if (id) {
      this.store.dispatch(FormBuilderActions.loadForm({ formId: id }));
      this.store.dispatch(FormBuilderActions.loadVersions({ formId: id }));
    }
  }

  ngOnDestroy(): void {
    this.store.dispatch(FormBuilderActions.reset());
  }

  protected controlsForCategory(category: FormControlCategory) {
    return this.registry.listByCategory(category);
  }

  protected componentLabel(component: FormComponent): string {
    return (
      resolveLocalizedText(component.label, this.currentLanguage(), 'en') ||
      component.variableKey ||
      component.type
    );
  }

  protected componentSupportsOptions(component: FormComponent): boolean {
    return this.registry.get(component.type)?.supportsOptions === true;
  }

  protected componentCollectsData(component: FormComponent): boolean {
    return this.registry.get(component.type)?.collectsData !== false;
  }

  protected onDraftNameInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.store.dispatch(FormBuilderActions.updateDraftName({ name: value }));
  }

  protected onLayoutModeChange(mode: FormLayoutMode): void {
    const schema = this.draftSchema();
    if (!schema) {
      return;
    }
    this.store.dispatch(
      FormBuilderActions.updateDraftSchema({
        schema: { ...schema, layoutMode: mode },
      }),
    );
  }

  protected onAddStep(): void {
    const schema = this.draftSchema();
    if (!schema) {
      return;
    }
    const nextNum = schema.steps.length + 1;
    const updated: FormSchema = {
      ...schema,
      layoutMode: 'wizard',
      steps: [
        ...schema.steps,
        {
          id: `step_${nextNum}_${Date.now().toString(36)}`,
          title: {
            en: `Step ${nextNum}`,
            de: `Schritt ${nextNum}`,
            pl: `Krok ${nextNum}`,
          },
          components: [],
        },
      ],
    };
    this.store.dispatch(FormBuilderActions.updateDraftSchema({ schema: updated }));
    this.store.dispatch(FormBuilderActions.selectStep({ stepIndex: updated.steps.length - 1 }));
    this.announce('formBuilder.a11y.stepAdded', { index: nextNum });
  }

  protected onSelectStep(stepIndex: number): void {
    this.store.dispatch(FormBuilderActions.selectStep({ stepIndex }));
  }

  protected onAddControl(type: FormControlType): void {
    const schema = this.draftSchema();
    const step = this.activeStep();
    if (!schema || !step) {
      return;
    }

    const newComp = this.registry.createComponent(type, this.componentCounter++);
    const stepIdx = this.selectedStepIndex();
    const updatedSteps = schema.steps.map((s, idx) =>
      idx === stepIdx ? { ...s, components: [...s.components, newComp] } : s,
    );

    this.store.dispatch(
      FormBuilderActions.updateDraftSchema({
        schema: { ...schema, steps: updatedSteps },
      }),
    );
    this.store.dispatch(FormBuilderActions.selectComponent({ componentId: newComp.id }));
    this.announce('formBuilder.a11y.fieldAdded', { label: this.componentLabel(newComp) });
  }

  protected onDropComponent(event: CdkDragDrop<FormComponent[]>): void {
    if (event.previousIndex === event.currentIndex) {
      return;
    }
    this.reorderComponent(event.previousIndex, event.currentIndex);
  }

  protected onMoveComponent(index: number, direction: -1 | 1): void {
    const step = this.activeStep();
    if (!step) {
      return;
    }
    const targetIndex = index + direction;
    if (targetIndex < 0 || targetIndex >= step.components.length) {
      return;
    }
    this.reorderComponent(index, targetIndex);
  }

  protected onRemoveComponent(componentId: string): void {
    const schema = this.draftSchema();
    const step = this.activeStep();
    if (!schema || !step) {
      return;
    }
    const removed = step.components.find((c) => c.id === componentId);
    const stepIdx = this.selectedStepIndex();
    const updatedSteps = schema.steps.map((s, idx) =>
      idx === stepIdx
        ? { ...s, components: s.components.filter((c) => c.id !== componentId) }
        : s,
    );

    this.store.dispatch(
      FormBuilderActions.updateDraftSchema({
        schema: { ...schema, steps: updatedSteps },
      }),
    );
    if (this.selectedComponentId() === componentId) {
      this.store.dispatch(FormBuilderActions.selectComponent({ componentId: null }));
    }
    if (removed) {
      this.announce('formBuilder.a11y.fieldRemoved', { label: this.componentLabel(removed) });
    }
  }

  protected onSelectComponent(componentId: string): void {
    this.store.dispatch(FormBuilderActions.selectComponent({ componentId }));
  }

  protected onInspectorVariableKeyChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.patchSelectedComponent((comp) => ({ ...comp, variableKey: value }));
  }

  protected onInspectorBooleanToggle(
    field: 'preFilled' | 'readOnly' | 'isSensitive',
    event: Event,
  ): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.patchSelectedComponent((comp) => ({ ...comp, [field]: checked }));
  }

  protected onInspectorLocalizedChange(
    field: 'label' | 'description' | 'placeholder',
    lang: SupportedLanguage,
    event: Event,
  ): void {
    const value = (event.target as HTMLInputElement).value;
    this.patchSelectedComponent((comp) => ({
      ...comp,
      [field]: {
        ...(comp[field] ?? {}),
        [lang]: value,
      },
    }));
  }

  protected onInspectorRuleChange(
    rule: 'visibleIf' | 'requiredIf' | 'readOnlyIf',
    event: Event,
  ): void {
    const value = (event.target as HTMLInputElement).value.trim();
    this.patchSelectedComponent((comp) => ({
      ...comp,
      rules: {
        ...(comp.rules ?? {}),
        [rule]: value.length > 0 ? value : undefined,
      },
    }));
  }

  protected hasValidator(type: FormValidatorType): boolean {
    const comp = this.selectedComponent();
    return (comp?.validators ?? []).some((v) => v.type === type);
  }

  protected getValidator(type: FormValidatorType): FormFieldValidator | undefined {
    const comp = this.selectedComponent();
    return (comp?.validators ?? []).find((v) => v.type === type);
  }

  protected onToggleClickableValidator(type: FormValidatorType, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.patchSelectedComponent((comp) => {
      const existing = comp.validators ?? [];
      if (!checked) {
        return { ...comp, validators: existing.filter((v) => v.type !== type) };
      }
      if (existing.some((v) => v.type === type)) {
        return comp;
      }
      const next: FormFieldValidator =
        type === 'compareField'
          ? { type, operator: 'gte', targetField: '' }
          : { type };
      return { ...comp, validators: [...existing, next] };
    });
  }

  protected onUpdateValidatorField(
    type: FormValidatorType,
    field: 'value' | 'pattern' | 'targetField' | 'operator' | 'expression',
    event: Event,
  ): void {
    const raw = (event.target as HTMLInputElement | HTMLSelectElement).value;
    this.patchSelectedComponent((comp) => {
      const existing = comp.validators ?? [];
      const found = existing.some((v) => v.type === type);
      const parsedVal =
        field === 'value' && raw.trim().length > 0 && !Number.isNaN(Number(raw))
          ? Number(raw)
          : raw;

      if (!found && type === 'expression') {
        if (raw.trim().length === 0) {
          return comp;
        }
        return {
          ...comp,
          validators: [...existing, { type: 'expression', expression: raw }],
        };
      }

      if (type === 'expression' && field === 'expression' && raw.trim().length === 0) {
        return {
          ...comp,
          validators: existing.filter((v) => v.type !== 'expression'),
        };
      }

      return {
        ...comp,
        validators: existing.map((v) => (v.type === type ? { ...v, [field]: parsedVal } : v)),
      };
    });
  }

  protected onUpdateExpressionErrorMessage(lang: SupportedLanguage, event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.patchSelectedComponent((comp) => {
      const existing = comp.validators ?? [];
      return {
        ...comp,
        validators: existing.map((v) =>
          v.type === 'expression'
            ? { ...v, message: { ...(v.message ?? {}), [lang]: value } }
            : v,
        ),
      };
    });
  }

  protected onAddOption(): void {
    this.patchSelectedComponent((comp) => {
      const current = comp.options ?? [];
      const idx = current.length + 1;
      return {
        ...comp,
        options: [
          ...current,
          {
            value: `option_${idx}`,
            label: { en: `Option ${idx}`, de: `Option ${idx}`, pl: `Opcja ${idx}` },
          },
        ],
      };
    });
  }

  protected onUpdateOption(
    optionIndex: number,
    field: 'value' | 'label',
    event: Event,
  ): void {
    const raw = (event.target as HTMLInputElement).value;
    const lang = this.inspectorLanguage();
    this.patchSelectedComponent((comp) => {
      const options = (comp.options ?? []).map((opt, idx) => {
        if (idx !== optionIndex) {
          return opt;
        }
        if (field === 'value') {
          return { ...opt, value: raw };
        }
        return { ...opt, label: { ...opt.label, [lang]: raw } };
      });
      return { ...comp, options };
    });
  }

  protected onRemoveOption(optionIndex: number): void {
    this.patchSelectedComponent((comp) => ({
      ...comp,
      options: (comp.options ?? []).filter((_, idx) => idx !== optionIndex),
    }));
  }

  protected onSaveDraft(): void {
    const id = this.formId();
    const schema = this.draftSchema();
    if (!id || !schema) {
      return;
    }
    this.store.dispatch(
      FormBuilderActions.saveDraft({
        formId: id,
        request: {
          name: this.draftName().trim() || undefined,
          draftSchema: schema,
        },
      }),
    );
  }

  protected onPublish(): void {
    const id = this.formId();
    if (!id) {
      return;
    }
    this.store.dispatch(FormBuilderActions.publishVersion({ formId: id }));
  }

  protected onPreviewVersion(schema: FormSchema): void {
    this.previewedVersionSchema.set(schema);
    this.activeTab.set('preview');
  }

  protected onClearVersionPreview(): void {
    this.previewedVersionSchema.set(null);
  }

  protected setActiveTab(tab: 'design' | 'preview' | 'versions'): void {
    if (tab !== 'preview') {
      this.previewedVersionSchema.set(null);
    }
    this.activeTab.set(tab);
  }

  private reorderComponent(fromIndex: number, toIndex: number): void {
    const schema = this.draftSchema();
    const step = this.activeStep();
    if (!schema || !step) {
      return;
    }
    const items = [...step.components];
    const moved = items[fromIndex];
    moveItemInArray(items, fromIndex, toIndex);

    const stepIdx = this.selectedStepIndex();
    const updatedSteps = schema.steps.map((s, idx) =>
      idx === stepIdx ? { ...s, components: items } : s,
    );

    this.store.dispatch(
      FormBuilderActions.updateDraftSchema({
        schema: { ...schema, steps: updatedSteps },
      }),
    );
    if (moved) {
      this.announce('formBuilder.a11y.fieldMoved', {
        label: this.componentLabel(moved),
        position: toIndex + 1,
      });
    }
  }

  private patchSelectedComponent(updater: (comp: FormComponent) => FormComponent): void {
    const schema = this.draftSchema();
    const selectedId = this.selectedComponentId();
    if (!schema || !selectedId) {
      return;
    }
    const stepIdx = this.selectedStepIndex();
    const updatedSteps = schema.steps.map((step, idx) =>
      idx === stepIdx
        ? {
            ...step,
            components: step.components.map((comp) =>
              comp.id === selectedId ? updater(comp) : comp,
            ),
          }
        : step,
    );

    this.store.dispatch(
      FormBuilderActions.updateDraftSchema({
        schema: { ...schema, steps: updatedSteps },
      }),
    );
  }

  private announce(key: string, params?: Readonly<Record<string, string | number>>): void {
    this.liveAnnouncement.set(
      this.translationService.translate(key, this.currentLanguage(), params),
    );
  }
}
