import { Component } from '@angular/core';
@Component({
  selector: 'app-support',
  standalone: true,
  imports: [],
  templateUrl: './support.component.html'
})
export class SupportComponent {
  supportItems = [
    {
      icon: 'help_center',
      title: 'Help Center',
      description: 'Browse our comprehensive help documentation',
      link: '#'
    },
    {
      icon: 'contact_support',
      title: 'Contact Support',
      description: 'Get in touch with our support team',
      link: '#'
    },
    {
      icon: 'bug_report',
      title: 'Report a Bug',
      description: 'Let us know if something isn\'t working',
      link: '#'
    },
    {
      icon: 'feedback',
      title: 'Send Feedback',
      description: 'Share your ideas and suggestions',
      link: '#'
    }
  ];

  getCurrentDate(): string {
    return new Date().toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }
}
