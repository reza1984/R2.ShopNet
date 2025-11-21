import {
  Component,
  Input,
  Output,
  EventEmitter,
  ElementRef,
  OnDestroy,
  OnChanges,
  HostListener
} from '@angular/core';
import { NgClass } from '@angular/common';
import { IconComponent } from '../../icon/icon.component';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [
    NgClass,
    IconComponent,
  ],
  templateUrl: './modal.component.html',
  styles: ``
})
export class ModalComponent implements OnDestroy, OnChanges {

  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();
  @Input() className = '';
  @Input() showCloseButton = true;
  @Input() isFullscreen = false;

  constructor(private el: ElementRef) {
    // Initial overflow handling is done in ngOnChanges
  }

  ngOnDestroy() {
    document.body.style.overflow = 'unset';
  }

  ngOnChanges() {
    document.body.style.overflow = this.isOpen ? 'hidden' : 'unset';
  }

  onBackdropClick(event: MouseEvent) {
    if (!this.isFullscreen) {
      this.close.emit();
    }
  }

  onContentClick(event: MouseEvent) {
    event.stopPropagation();
  }

  @HostListener('document:keydown.escape', ['$event'])
  onEscape(event: Event) {
    if (this.isOpen) {
      this.close.emit();
    }
  }
}
