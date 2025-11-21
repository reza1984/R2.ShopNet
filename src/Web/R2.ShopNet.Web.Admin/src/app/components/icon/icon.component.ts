import { Component, Input } from '@angular/core';
type IconStyle = 'material' | 'symbols-outlined' | 'symbols-rounded' | 'symbols-sharp';

@Component({
  selector: 'app-icon',
  standalone: true,
  imports: [],
  template: `
    <span 
      [class]="iconClass"
      [style.font-size.px]="size"
      [style.color]="color">
      {{ name }}
    </span>
  `,
  styles: [`
    :host {
      display: flex;
      align-items: center;
      justify-content: center;
    }
  `]
})
export class IconComponent {
  @Input() name!: string;
  @Input() style: IconStyle = 'symbols-rounded';
  @Input() size?: number;
  @Input() color?: string;
  @Input() filled: boolean = false;

  get iconClass(): string {
    const baseClasses: Record<IconStyle, string> = {
      'material': 'material-icons',
      'symbols-outlined': 'material-symbols-rounded',
      'symbols-rounded': 'material-symbols-rounded',
      'symbols-sharp': 'material-symbols-rounded'
    };

    const classes = [baseClasses[this.style]];
    
    if (this.filled && this.style !== 'material') {
      classes.push('material-symbols-filled');
    }

    return classes.join(' ');
  }
}
