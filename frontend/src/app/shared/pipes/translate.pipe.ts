import { inject, Pipe, PipeTransform } from '@angular/core';
import { Store } from '@ngrx/store';
import { TranslationService } from '../../core/i18n/services/translation.service';
import { selectCurrentLanguage } from '../../core/i18n/store/i18n.selectors';

@Pipe({
  name: 'translate',
  standalone: true,
  pure: false,
})
export class TranslatePipe implements PipeTransform {
  private readonly store = inject(Store);
  private readonly translationService = inject(TranslationService);
  private readonly language = this.store.selectSignal(selectCurrentLanguage);

  transform(key: string, params?: Readonly<Record<string, string | number>>): string {
    return this.translationService.translate(key, this.language(), params);
  }
}
