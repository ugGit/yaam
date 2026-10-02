import { Component, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

interface NavItem {
  label: string;
  route: string;
  disabled?: boolean;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
  host: { class: 'flex flex-col min-h-full w-64 bg-base-200' },
})
export class SidebarComponent {
  readonly navigated = output<void>();

  protected readonly mainItems: NavItem[] = [
    { label: 'Applications', route: '/applications' },
    { label: 'Profile', route: '/profile' },
    { label: 'Cover Letters', route: '/cover-letters', disabled: true },
    { label: 'Reminders', route: '/reminders' },
  ];

  // Temporary until the account menu from the Account Management feature replaces it.
  protected readonly secondaryItems: NavItem[] = [{ label: 'Account', route: '/account' }];
}
