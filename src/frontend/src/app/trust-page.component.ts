import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ActivatedRoute } from '@angular/router';

export type TrustPageContent = {
  eyebrow: string;
  title: string;
  intro: string;
  sections: { title: string; paragraphs: string[]; bullets?: string[] }[];
};

@Component({
  selector: 'app-trust-page',
  standalone: true,
  imports: [RouterLink],
  styleUrl: './trust-page.component.scss',
  templateUrl: './trust-page.component.html',
})
export class TrustPageComponent {
  readonly page = inject(ActivatedRoute).snapshot.data['page'] as TrustPageContent;
}
