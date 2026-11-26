import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { SyncStatus } from '../../../core/connectivity/models/sync-status.type';
import { TranslatePipe } from '../../pipes/translate.pipe';

@Component({
  selector: 'app-connectivity-banner',
  standalone: true,
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './connectivity-banner.component.html',
})
export class ConnectivityBannerComponent {
  readonly isOnline = input.required<boolean>();
  readonly pendingDraftsCount = input<number>(0);
  readonly syncStatus = input<SyncStatus>('idle');
}
