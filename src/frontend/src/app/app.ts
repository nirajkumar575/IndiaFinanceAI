import { HttpClient } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { AdminDashboardComponent } from './admin-dashboard.component';
import {
  AssistantLocale,
  AssistantState,
  LOAN_ASSISTANT_ENGINE,
} from './loan-assistant-engine';

type LoanProduct = {
  name: string;
  description: string;
};

type ChatMessage = {
  id: number;
  role: 'assistant' | 'user';
  text: string;
};

@Component({
  imports: [ReactiveFormsModule, AdminDashboardComponent, RouterOutlet, RouterLink],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  private readonly http = inject(HttpClient);
  private readonly formBuilder = inject(FormBuilder).nonNullable;
  private readonly assistant = inject(LOAN_ASSISTANT_ENGINE);
  private readonly router = inject(Router);
  private readonly leadsUrl = '/api/leads';

  readonly loanProducts: LoanProduct[] = [
    { name: 'Personal Loan', description: 'For planned expenses, life events and personal goals.' },
    { name: 'Business Loan', description: 'Working capital and business needs for growing enterprises.' },
    { name: 'MSME Loan', description: 'Financing options designed for eligible small businesses.' },
    { name: 'Home Loan', description: 'Explore financing for a home purchase or construction.' },
    { name: 'Vehicle Loan', description: 'Options to help finance a new or pre-owned vehicle.' },
    { name: 'Loan Against Property', description: 'Explore secured lending against eligible property.' },
  ];

  readonly leadForm = this.formBuilder.group({
    fullName: ['', [Validators.required, Validators.maxLength(100)]],
    mobileNumber: ['', [Validators.required, Validators.pattern('^[6-9][0-9]{9}$')]],
    email: ['', [Validators.email, Validators.maxLength(254)]],
    state: ['', [Validators.required, Validators.maxLength(100)]],
    city: ['', [Validators.required, Validators.maxLength(100)]],
    loanType: ['', Validators.required],
    requiredLoanAmount: [500000, [Validators.required, Validators.min(10000), Validators.max(50000000)]],
    monthlyIncome: [50000, [Validators.required, Validators.min(1), Validators.max(100000000)]],
    employmentType: ['', Validators.required],
    consentGiven: [false, Validators.requiredTrue],
    whatsAppConsentGiven: [false],
    callbackRequested: [false],
    preferredContactTime: [''],
  });

  isSubmitting = false;
  submitted = false;
  feedback = '';
  feedbackKind: 'success' | 'error' = 'error';
  adminDashboardOpen = false;
  chatOpen = false;
  chatLocale: AssistantLocale = 'en';
  chatDraft = '';
  assistantTyping = false;
  assistantSubmitting = false;
  assistantState: AssistantState = this.assistant.initialState();
  chatMessages: ChatMessage[] = [{ id: 0, role: 'assistant', text: this.assistant.greeting(this.chatLocale) }];
  private nextChatMessageId = 1;

  get isHomeRoute(): boolean {
    return this.router.url === '/' || this.router.url.startsWith('/#');
  }

  get assistantSuggestions(): string[] {
    if (this.assistantState.step === 'loanType') return this.loanProducts.map(product => product.name);
    if (this.assistantState.step === 'employment') {
      return this.chatLocale === 'hi' ? ['वेतनभोगी', 'स्वरोजगार'] : ['Salaried', 'Self-employed'];
    }
    if (this.assistantState.step === 'preferredTime') {
      return this.chatLocale === 'hi' ? ['सुबह', 'दोपहर', 'शाम', 'कोई भी समय'] : ['Morning', 'Afternoon', 'Evening', 'Any time'];
    }
    if (this.assistantState.step === 'contact' || this.assistantState.step === 'consent') {
      return this.chatLocale === 'hi' ? ['हाँ', 'नहीं'] : ['Yes', 'No'];
    }
    if (this.assistantState.step === 'whatsAppConsent') {
      return this.chatLocale === 'hi' ? ['हाँ', 'नहीं'] : ['Yes', 'No'];
    }
    return [];
  }

  selectLoan(loanType: string): void {
    this.leadForm.controls.loanType.setValue(loanType);
    document.getElementById('enquiry')?.scrollIntoView?.({ behavior: 'smooth' });
  }

  showError(controlName: string): boolean {
    const control = this.leadForm.get(controlName);
    return Boolean(control && control.invalid && (control.touched || this.submitted));
  }

  submitEnquiry(): void {
    this.submitted = true;
    if (this.leadForm.invalid) {
      this.leadForm.markAllAsTouched();
      this.feedback = 'Please check the required fields and consent before submitting.';
      this.feedbackKind = 'error';
      return;
    }

    this.isSubmitting = true;
    this.feedback = '';
    const formValue = this.leadForm.getRawValue();
    this.http.post(this.leadsUrl, {
      ...formValue,
      preferredContactTime: formValue.preferredContactTime || null,
      whatsAppConsentGiven: formValue.whatsAppConsentGiven,
      leadSource: 'Website',
    }).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.feedback = 'Your enquiry has been submitted. Our team can follow up with you.';
        this.feedbackKind = 'success';
        this.leadForm.reset({
          fullName: '',
          mobileNumber: '',
          email: '',
          state: '',
          city: '',
          loanType: '',
          requiredLoanAmount: 500000,
          monthlyIncome: 50000,
          employmentType: '',
          consentGiven: false,
          whatsAppConsentGiven: false,
          callbackRequested: false,
          preferredContactTime: '',
        });
        this.submitted = false;
      },
      error: () => {
        this.isSubmitting = false;
        this.feedback = 'We could not submit your enquiry right now. Please try again later.';
        this.feedbackKind = 'error';
      },
    });
  }

  sendChatMessage(message = this.chatDraft): void {
    const text = message.trim();
    if (!text || this.assistantTyping || this.assistantSubmitting) return;

    this.chatMessages.push({ id: this.nextChatMessageId++, role: 'user', text });
    this.chatDraft = '';
    this.assistantTyping = true;
    this.scrollChatToEnd();

    window.setTimeout(() => {
      const turn = this.assistant.respond(text, this.assistantState, this.chatLocale);
      this.assistantState = turn.state;
      this.chatMessages.push({ id: this.nextChatMessageId++, role: 'assistant', text: turn.reply });
      this.scrollChatToEnd();
      if (turn.submitLead) {
        this.submitAssistantLead();
      } else {
        this.assistantTyping = false;
      }
    }, 350);
  }

  setChatLocale(locale: AssistantLocale): void {
    if (this.chatLocale === locale) return;
    this.chatLocale = locale;
    if (this.assistantState.step !== 'done') {
      this.chatMessages.push({
        id: this.nextChatMessageId++,
        role: 'assistant',
        text: this.assistant.currentQuestion(this.assistantState, locale),
      });
      this.scrollChatToEnd();
    }
  }

  restartAssistant(): void {
    this.assistantState = this.assistant.initialState();
    this.assistantTyping = false;
    this.assistantSubmitting = false;
    this.chatMessages = [{ id: this.nextChatMessageId++, role: 'assistant', text: this.assistant.greeting(this.chatLocale) }];
  }

  private submitAssistantLead(): void {
    const lead = this.assistantState.lead;
    this.assistantSubmitting = true;
    this.http.post<{ id: string }>(this.leadsUrl, {
      fullName: lead.fullName,
      mobileNumber: lead.mobileNumber,
      email: null,
      state: lead.state,
      city: lead.city,
      loanType: lead.loanType,
      requiredLoanAmount: lead.requiredLoanAmount,
      monthlyIncome: lead.monthlyIncome,
      employmentType: lead.employmentType,
      consentGiven: true,
      callbackRequested: lead.callbackRequested ?? false,
      preferredContactTime: lead.preferredContactTime ?? null,
      leadSource: 'AI Assistant',
      whatsAppConsentGiven: lead.whatsAppConsentGiven ?? false,
    }).subscribe({
      next: () => {
        this.assistantSubmitting = false;
        this.assistantTyping = false;
        this.chatMessages.push({
          id: this.nextChatMessageId++,
          role: 'assistant',
          text: this.chatLocale === 'hi'
            ? 'धन्यवाद! आपकी enquiry भेज दी गई है। हमारी टीम आपसे संपर्क कर सकती है। यह loan approval नहीं है; अंतिम निर्णय lender लेता है।'
            : 'Thank you! Your enquiry has been sent. Our team may contact you. This is not a loan approval; the lender makes the final decision.',
        });
        this.scrollChatToEnd();
      },
      error: () => {
        this.assistantSubmitting = false;
        this.assistantTyping = false;
        this.assistantState = { ...this.assistantState, step: 'consent' };
        this.chatMessages.push({
          id: this.nextChatMessageId++,
          role: 'assistant',
          text: this.chatLocale === 'hi'
            ? 'अभी enquiry भेजी नहीं जा सकी। आपकी जानकारी सेव नहीं हुई। कृपया “नई chat” से फिर कोशिश करें।'
            : 'The enquiry could not be sent, so your information was not saved. Please try again with “New chat”.',
        });
        this.scrollChatToEnd();
      },
    });
  }

  private scrollChatToEnd(): void {
    window.setTimeout(() => {
      const messageLog = document.querySelector<HTMLElement>('.chat-messages');
      if (messageLog) messageLog.scrollTop = messageLog.scrollHeight;
    });
  }
}
