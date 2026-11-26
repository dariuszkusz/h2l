import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { Store } from '@ngrx/store';
import { FormsApiService } from '../../../core/api/forms-api.service';
import { components } from '../../../core/api/generated/openapi.types';
import { MappedApiError } from '../../../core/api/models/mapped-api-error.model';
import { ProblemDetailsMapperService } from '../../../core/api/services/problem-details-mapper.service';
import { TranslationService } from '../../../core/i18n/services/translation.service';
import { selectCurrentLanguage } from '../../../core/i18n/store/i18n.selectors';
import { TranslatePipe } from '../../pipes/translate.pipe';
import { HlmButtonDirective } from '../../ui/hlm-button.directive';
import { HlmInputDirective } from '../../ui/hlm-input.directive';
import { HlmLabelDirective } from '../../ui/hlm-label.directive';
import { UiStateBannerComponent } from '../ui-state-banner/ui-state-banner.component';

type N8nTemplateVariant = components['schemas']['N8nTemplateVariant'];

@Component({
  selector: 'app-workflow-template-export',
  standalone: true,
  imports: [
    TranslatePipe,
    HlmButtonDirective,
    HlmInputDirective,
    HlmLabelDirective,
    UiStateBannerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './workflow-template-export.component.html',
})
export class WorkflowTemplateExportComponent {
  private readonly formsApi = inject(FormsApiService);
  private readonly problemDetailsMapper = inject(ProblemDetailsMapperService);
  private readonly translationService = inject(TranslationService);
  private readonly store = inject(Store);
  private readonly document = inject(DOCUMENT);

  readonly formId = input.required<string>();
  readonly stepKey = input<string>('');

  protected readonly currentLanguage = this.store.selectSignal(selectCurrentLanguage);
  protected readonly n8nVariant = signal<N8nTemplateVariant>('community-node');
  protected readonly downloadingCamunda = signal(false);
  protected readonly copyingN8n = signal(false);
  protected readonly feedbackMessage = signal<string | null>(null);
  protected readonly error = signal<MappedApiError | null>(null);

  protected readonly n8nVariants: readonly N8nTemplateVariant[] = [
    'community-node',
    'fallback-wait-node',
  ];

  protected onVariantChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.n8nVariant.set(select.value as N8nTemplateVariant);
    this.feedbackMessage.set(null);
    this.error.set(null);
  }

  protected onDownloadCamundaTemplate(): void {
    const id = this.formId();
    if (!id || this.downloadingCamunda()) {
      return;
    }

    this.downloadingCamunda.set(true);
    this.error.set(null);
    this.feedbackMessage.set(null);

    this.formsApi.getCamundaElementTemplate(id).subscribe({
      next: (templateJson) => {
        this.downloadingCamunda.set(false);
        const safeSlug = (this.stepKey().trim() || id).replace(/[^a-zA-Z0-9_-]/g, '-');
        const fileName = `h2l-camunda-template-${safeSlug}.json`;
        this.triggerJsonDownload(fileName, templateJson);
        this.feedbackMessage.set(
          this.translationService.translate(
            'workflowTemplates.camundaDownloaded',
            this.currentLanguage(),
            { fileName },
          ),
        );
      },
      error: (err: unknown) => {
        this.downloadingCamunda.set(false);
        this.error.set(this.problemDetailsMapper.map(err));
      },
    });
  }

  protected onCopyN8nTemplate(): void {
    const id = this.formId();
    if (!id || this.copyingN8n()) {
      return;
    }

    const variant = this.n8nVariant();
    this.copyingN8n.set(true);
    this.error.set(null);
    this.feedbackMessage.set(null);

    this.formsApi.getN8nNodesTemplate(id, { variant }).subscribe({
      next: (response) => {
        const serialized = JSON.stringify(response.workflowJson, null, 2);
        void this.copyTextToClipboard(serialized).then(() => {
          this.copyingN8n.set(false);
          const key =
            variant === 'community-node'
              ? 'workflowTemplates.n8nCopiedCommunity'
              : 'workflowTemplates.n8nCopiedFallback';
          this.feedbackMessage.set(
            this.translationService.translate(key, this.currentLanguage()),
          );
        });
      },
      error: (err: unknown) => {
        this.copyingN8n.set(false);
        this.error.set(this.problemDetailsMapper.map(err));
      },
    });
  }

  private triggerJsonDownload(fileName: string, payload: unknown): void {
    const json = JSON.stringify(payload, null, 2);
    const blob = new Blob([json], { type: 'application/json;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const anchor = this.document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    anchor.rel = 'noopener';
    this.document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
  }

  private async copyTextToClipboard(text: string): Promise<void> {
    const nav = this.document.defaultView?.navigator;
    if (nav?.clipboard?.writeText) {
      try {
        await nav.clipboard.writeText(text);
        return;
      } catch {
        // Fallback below for restricted clipboard contexts
      }
    }

    const textarea = this.document.createElement('textarea');
    textarea.value = text;
    textarea.setAttribute('readonly', '');
    textarea.style.position = 'fixed';
    textarea.style.opacity = '0';
    this.document.body.appendChild(textarea);
    textarea.select();
    this.document.execCommand('copy');
    textarea.remove();
  }
}
