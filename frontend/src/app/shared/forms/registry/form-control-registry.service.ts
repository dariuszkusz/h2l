import { inject, Injectable } from '@angular/core';
import { components } from '../../../core/api/generated/openapi.types';
import { FormControlType } from '../utils/form-control-type.type';
import { FormControlCategory } from './form-control-category.type';
import { FormControlDefinition } from './form-control-definition.model';
import { FORM_CONTROL_DEFINITIONS } from './form-control-registry.token';

type FormComponent = components['schemas']['FormComponent'];
type OpenApiType = components['schemas']['FormComponentType'];

function buildBaseComponent(
  type: FormControlType,
  index: number,
  collectsData: boolean,
  labelEn: string,
  labelDe: string,
  labelPl: string,
): FormComponent {
  const id = `cmp_${type}_${index}_${Date.now().toString(36)}`;
  return {
    id,
    type: type as OpenApiType,
    ...(collectsData ? { variableKey: `${type.replaceAll('_', '')}${index}` } : {}),
    label: {
      en: labelEn,
      de: labelDe,
      pl: labelPl,
    },
    preFilled: false,
    readOnly: false,
    isSensitive: false,
    validators: [],
    rules: {},
  };
}

const BUILTIN_CONTROLS: readonly FormControlDefinition[] = [
  {
    type: 'text',
    category: 'data',
    labelKey: 'formBuilder.controls.text',
    collectsData: true,
    supportsOptions: false,
    supportsChildren: false,
    defaultValue: '',
    createDefaultComponent: (i) => buildBaseComponent('text', i, true, 'Text field', 'Textfeld', 'Pole tekstowe'),
  },
  {
    type: 'textarea',
    category: 'data',
    labelKey: 'formBuilder.controls.textarea',
    collectsData: true,
    supportsOptions: false,
    supportsChildren: false,
    defaultValue: '',
    createDefaultComponent: (i) => buildBaseComponent('textarea', i, true, 'Long text', 'Mehrzeiliger Text', 'Dłuższy tekst'),
  },
  {
    type: 'number',
    category: 'data',
    labelKey: 'formBuilder.controls.number',
    collectsData: true,
    supportsOptions: false,
    supportsChildren: false,
    defaultValue: null,
    createDefaultComponent: (i) => buildBaseComponent('number', i, true, 'Number', 'Zahl', 'Liczba'),
  },
  {
    type: 'email',
    category: 'data',
    labelKey: 'formBuilder.controls.email',
    collectsData: true,
    supportsOptions: false,
    supportsChildren: false,
    defaultValue: '',
    createDefaultComponent: (i) => ({
      ...buildBaseComponent('email', i, true, 'Email address', 'E-Mail-Adresse', 'Adres e-mail'),
      validators: [{ type: 'email' }],
    }),
  },
  {
    type: 'phone',
    category: 'data',
    labelKey: 'formBuilder.controls.phone',
    collectsData: true,
    supportsOptions: false,
    supportsChildren: false,
    defaultValue: '',
    createDefaultComponent: (i) => buildBaseComponent('phone', i, true, 'Phone number', 'Telefonnummer', 'Numer telefonu'),
  },
  {
    type: 'url',
    category: 'data',
    labelKey: 'formBuilder.controls.url',
    collectsData: true,
    supportsOptions: false,
    supportsChildren: false,
    defaultValue: '',
    createDefaultComponent: (i) => buildBaseComponent('url', i, true, 'URL', 'URL', 'Adres URL'),
  },
  {
    type: 'date',
    category: 'data',
    labelKey: 'formBuilder.controls.date',
    collectsData: true,
    supportsOptions: false,
    supportsChildren: false,
    defaultValue: '',
    createDefaultComponent: (i) => buildBaseComponent('date', i, true, 'Date', 'Datum', 'Data'),
  },
  {
    type: 'time',
    category: 'data',
    labelKey: 'formBuilder.controls.time',
    collectsData: true,
    supportsOptions: false,
    supportsChildren: false,
    defaultValue: '',
    createDefaultComponent: (i) => buildBaseComponent('time', i, true, 'Time', 'Uhrzeit', 'Godzina'),
  },
  {
    type: 'datetime',
    category: 'data',
    labelKey: 'formBuilder.controls.datetime',
    collectsData: true,
    supportsOptions: false,
    supportsChildren: false,
    defaultValue: '',
    createDefaultComponent: (i) => buildBaseComponent('datetime', i, true, 'Date & time', 'Datum & Uhrzeit', 'Data i godzina'),
  },
  {
    type: 'select',
    category: 'choice',
    labelKey: 'formBuilder.controls.select',
    collectsData: true,
    supportsOptions: true,
    supportsChildren: false,
    defaultValue: '',
    createDefaultComponent: (i) => ({
      ...buildBaseComponent('select', i, true, 'Select', 'Auswahl', 'Lista wyboru'),
      options: [
        { value: 'option_1', label: { en: 'Option 1', de: 'Option 1', pl: 'Opcja 1' } },
        { value: 'option_2', label: { en: 'Option 2', de: 'Option 2', pl: 'Opcja 2' } },
      ],
    }),
  },
  {
    type: 'radio',
    category: 'choice',
    labelKey: 'formBuilder.controls.radio',
    collectsData: true,
    supportsOptions: true,
    supportsChildren: false,
    defaultValue: '',
    createDefaultComponent: (i) => ({
      ...buildBaseComponent('radio', i, true, 'Radio group', 'Optionsgruppe', 'Wybór jednokrotny'),
      options: [
        { value: 'yes', label: { en: 'Yes', de: 'Ja', pl: 'Tak' } },
        { value: 'no', label: { en: 'No', de: 'Nein', pl: 'Nie' } },
      ],
    }),
  },
  {
    type: 'checkbox',
    category: 'choice',
    labelKey: 'formBuilder.controls.checkbox',
    collectsData: true,
    supportsOptions: false,
    supportsChildren: false,
    defaultValue: false,
    createDefaultComponent: (i) => buildBaseComponent('checkbox', i, true, 'Checkbox', 'Kontrollkästchen', 'Pole wyboru'),
  },
  {
    type: 'checkbox_group',
    category: 'choice',
    labelKey: 'formBuilder.controls.checkbox_group',
    collectsData: true,
    supportsOptions: true,
    supportsChildren: false,
    defaultValue: [],
    createDefaultComponent: (i) => ({
      ...buildBaseComponent('checkbox_group', i, true, 'Checkbox group', 'Mehrfachauswahl', 'Wybór wielokrotny'),
      options: [
        { value: 'item_a', label: { en: 'Item A', de: 'Punkt A', pl: 'Pozycja A' } },
        { value: 'item_b', label: { en: 'Item B', de: 'Punkt B', pl: 'Pozycja B' } },
      ],
    }),
  },
  {
    type: 'toggle',
    category: 'choice',
    labelKey: 'formBuilder.controls.toggle',
    collectsData: true,
    supportsOptions: false,
    supportsChildren: false,
    defaultValue: false,
    createDefaultComponent: (i) => buildBaseComponent('toggle', i, true, 'Toggle switch', 'Schalter', 'Przełącznik'),
  },
  {
    type: 'file',
    category: 'advanced',
    labelKey: 'formBuilder.controls.file',
    collectsData: true,
    supportsOptions: false,
    supportsChildren: false,
    defaultValue: null,
    createDefaultComponent: (i) => ({
      ...buildBaseComponent('file', i, true, 'File attachment', 'Dateianhang', 'Załącznik'),
      fileConfig: {
        allowedMimeTypes: ['application/pdf', 'image/png', 'image/jpeg'],
        maxSizeBytes: 10485760,
        multiple: false,
      },
    }),
  },
  {
    type: 'repeater',
    category: 'advanced',
    labelKey: 'formBuilder.controls.repeater',
    collectsData: true,
    supportsOptions: false,
    supportsChildren: true,
    defaultValue: [],
    createDefaultComponent: (i) => ({
      ...buildBaseComponent('repeater', i, true, 'Repeater list', 'Wiederholungsliste', 'Lista powtarzalna'),
      components: [
        buildBaseComponent('text', i * 10 + 1, true, 'Item name', 'Bezeichnung', 'Nazwa pozycji'),
      ],
    }),
  },
  {
    type: 'section',
    category: 'layout',
    labelKey: 'formBuilder.controls.section',
    collectsData: false,
    supportsOptions: false,
    supportsChildren: true,
    defaultValue: undefined,
    createDefaultComponent: (i) => ({
      ...buildBaseComponent('section', i, false, 'Section', 'Abschnitt', 'Sekcja'),
      components: [],
    }),
  },
  {
    type: 'grid',
    category: 'layout',
    labelKey: 'formBuilder.controls.grid',
    collectsData: false,
    supportsOptions: false,
    supportsChildren: true,
    defaultValue: undefined,
    createDefaultComponent: (i) => ({
      ...buildBaseComponent('grid', i, false, 'Columns grid', 'Spaltenraster', 'Siatka kolumn'),
      columns: 2,
      components: [],
    }),
  },
  {
    type: 'heading',
    category: 'layout',
    labelKey: 'formBuilder.controls.heading',
    collectsData: false,
    supportsOptions: false,
    supportsChildren: false,
    defaultValue: undefined,
    createDefaultComponent: (i) => buildBaseComponent('heading', i, false, 'Section heading', 'Überschrift', 'Nagłówek'),
  },
  {
    type: 'text_block',
    category: 'layout',
    labelKey: 'formBuilder.controls.text_block',
    collectsData: false,
    supportsOptions: false,
    supportsChildren: false,
    defaultValue: undefined,
    createDefaultComponent: (i) => ({
      ...buildBaseComponent('text_block', i, false, 'Information block', 'Infotext', 'Blok informacyjny'),
      description: {
        en: 'Helpful context for the person filling out this form.',
        de: 'Hilfreicher Hinweis für das Ausfüllen dieses Formulars.',
        pl: 'Pomocny opis dla osoby wypełniającej formularz.',
      },
    }),
  },
  {
    type: 'divider',
    category: 'layout',
    labelKey: 'formBuilder.controls.divider',
    collectsData: false,
    supportsOptions: false,
    supportsChildren: false,
    defaultValue: undefined,
    createDefaultComponent: (i) => buildBaseComponent('divider', i, false, 'Divider', 'Trennlinie', 'Separator'),
  },
];

@Injectable({
  providedIn: 'root',
})
export class FormControlRegistryService {
  private readonly customDefinitions = inject(FORM_CONTROL_DEFINITIONS, { optional: true }) ?? [];
  private readonly registry = new Map<string, FormControlDefinition>();

  constructor() {
    for (const def of BUILTIN_CONTROLS) {
      this.registry.set(def.type, def);
    }
    for (const def of this.customDefinitions) {
      this.registry.set(def.type, def);
    }
  }

  register(definition: FormControlDefinition): void {
    this.registry.set(definition.type, definition);
  }

  get(type: string): FormControlDefinition | undefined {
    return this.registry.get(type);
  }

  listAll(): readonly FormControlDefinition[] {
    return Array.from(this.registry.values());
  }

  listByCategory(category: FormControlCategory): readonly FormControlDefinition[] {
    return this.listAll().filter((def) => def.category === category);
  }

  createComponent(type: FormControlType, index: number): FormComponent {
    const def = this.registry.get(type);
    if (!def) {
      return buildBaseComponent('text', index, true, 'Text field', 'Textfeld', 'Pole tekstowe');
    }
    return def.createDefaultComponent(index);
  }
}
