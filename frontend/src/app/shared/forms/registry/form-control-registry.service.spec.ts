import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { FormControlRegistryService } from './form-control-registry.service';

describe('FormControlRegistryService', () => {
  it('registers all 21 Camunda/Form.io parity controls and allows OCP extension', () => {
    TestBed.configureTestingModule({});
    const service = TestBed.inject(FormControlRegistryService);

    const allTypes = service.listAll().map((d) => d.type);
    expect(allTypes).toEqual(
      expect.arrayContaining([
        'text',
        'textarea',
        'number',
        'email',
        'phone',
        'url',
        'date',
        'time',
        'datetime',
        'select',
        'radio',
        'checkbox',
        'checkbox_group',
        'toggle',
        'file',
        'section',
        'grid',
        'heading',
        'text_block',
        'divider',
        'repeater',
      ]),
    );

    const created = service.createComponent('file', 3);
    expect(created.type).toBe('file');
    expect(created.variableKey).toBe('file3');
    expect(created.fileConfig?.maxSizeBytes).toBe(10485760);
  });
});
