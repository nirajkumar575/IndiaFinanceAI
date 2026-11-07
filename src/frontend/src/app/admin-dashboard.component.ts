import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Component, EventEmitter, inject, Output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

type AdminLead = {
  id: string;
  fullName: string;
  city: string;
  state: string;
  loanType: string;
  requiredLoanAmount: number;
  createdAt: string;
  leadStatus: string;
  leadSource: string;
  followUpDate: string | null;
  whatsAppConsentGiven: boolean;
  whatsAppConsentAt: string | null;
  whatsAppOptedOutAt: string | null;
  lastWhatsAppActivityAt: string | null;
  lastWhatsAppStatus: string | null;
  score: number;
  priority: 'High' | 'Medium' | 'Low';
};

type AdminLeadDetails = AdminLead & {
  mobileNumber: string;
  email: string | null;
  monthlyIncome: number;
  employmentType: string;
  consentGiven: boolean;
  callbackRequested: boolean;
  preferredContactTime: string | null;
  followUpNotes: string | null;
  scoreSignals: { name: string; points: number; applied: boolean }[];
  aiSummary: string;
};

type WhatsAppIntegrationStatus = {
  cloudApiEnabled: boolean;
  webhookEnabled: boolean;
  approvedTemplates: string[];
};

type WhatsAppEvent = {
  id: string;
  eventType: string;
  status: string;
  templateName: string | null;
  errorCode: string | null;
  createdAt: string;
};

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  styleUrl: './admin-dashboard.component.scss',
  templateUrl: './admin-dashboard.component.html',
})
export class AdminDashboardComponent {
  @Output() readonly close = new EventEmitter<void>();

  private readonly http = inject(HttpClient);
  private readonly formBuilder = inject(FormBuilder).nonNullable;

  readonly loginForm = this.formBuilder.group({
    username: ['', Validators.required],
    password: ['', Validators.required],
  });
  readonly managementForm = this.formBuilder.group({
    leadStatus: ['New', Validators.required],
    followUpDate: [''],
    followUpNotes: ['', Validators.maxLength(2000)],
  });

  readonly loanTypes = ['Personal Loan', 'Business Loan', 'MSME Loan', 'Home Loan', 'Vehicle Loan', 'Loan Against Property'];
  readonly statuses = ['New', 'Contacted', 'Qualified', 'Closed', 'Disqualified'];
  accessToken = '';
  leads: AdminLead[] = [];
  selectedLead: AdminLeadDetails | null = null;
  whatsAppStatus: WhatsAppIntegrationStatus = { cloudApiEnabled: false, webhookEnabled: false, approvedTemplates: [] };
  whatsAppEvents: WhatsAppEvent[] = [];
  selectedTemplate = '';
  manualChatLink = '';
  whatsAppFeedback = '';
  whatsAppFeedbackKind: 'success' | 'error' = 'success';
  isWhatsAppBusy = false;
  loanTypeFilter = '';
  statusFilter = '';
  searchText = '';
  isLoading = false;
  isSaving = false;
  errorMessage = '';
  infoMessage = '';

  login(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.http.post<{ accessToken: string }>('/api/auth/admin/login', this.loginForm.getRawValue()).subscribe({
      next: response => {
        this.accessToken = response.accessToken;
        this.loginForm.reset();
        this.loadWhatsAppStatus();
        this.loadLeads();
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Login failed. Check your credentials and try again.';
      },
    });
  }

  loadLeads(): void {
    if (!this.accessToken) return;
    let params = new HttpParams();
    if (this.loanTypeFilter) params = params.set('loanType', this.loanTypeFilter);
    if (this.statusFilter) params = params.set('leadStatus', this.statusFilter);
    if (this.searchText.trim()) params = params.set('search', this.searchText.trim());

    this.isLoading = true;
    this.errorMessage = '';
    this.http.get<AdminLead[]>('/api/admin/leads', { params, headers: this.authHeaders() }).subscribe({
      next: leads => {
        this.leads = leads;
        this.isLoading = false;
        this.infoMessage = `${leads.length} leads found`;
        if (this.selectedLead && !leads.some(lead => lead.id === this.selectedLead?.id)) this.selectedLead = null;
      },
      error: () => this.handleRequestError(),
    });
  }

  openLead(id: string): void {
    this.http.get<AdminLeadDetails>(`/api/admin/leads/${id}`, { headers: this.authHeaders() }).subscribe({
      next: lead => {
        this.selectedLead = lead;
        this.manualChatLink = '';
        this.whatsAppFeedback = '';
        this.managementForm.setValue({
          leadStatus: lead.leadStatus,
          followUpDate: lead.followUpDate ?? '',
          followUpNotes: lead.followUpNotes ?? '',
        });
        this.loadWhatsAppEvents(id);
      },
      error: () => this.handleRequestError(),
    });
  }

  saveManagement(): void {
    if (!this.selectedLead || this.managementForm.invalid) {
      this.managementForm.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';
    this.http.put<void>(`/api/admin/leads/${this.selectedLead.id}/management`, this.managementForm.getRawValue(), { headers: this.authHeaders() }).subscribe({
      next: () => {
        const leadId = this.selectedLead?.id;
        this.isSaving = false;
        this.infoMessage = 'Lead management details saved.';
        this.loadLeads();
        if (leadId) this.openLead(leadId);
      },
      error: () => {
        this.isSaving = false;
        this.handleRequestError();
      },
    });
  }

  logout(): void {
    this.accessToken = '';
    this.leads = [];
    this.selectedLead = null;
    this.whatsAppEvents = [];
    this.manualChatLink = '';
    this.whatsAppStatus = { cloudApiEnabled: false, webhookEnabled: false, approvedTemplates: [] };
    this.managementForm.reset({ leadStatus: 'New', followUpDate: '', followUpNotes: '' });
  }

  prepareWhatsAppLink(): void {
    if (!this.selectedLead || !this.canContactOnWhatsApp()) return;
    this.isWhatsAppBusy = true;
    this.whatsAppFeedback = '';
    this.http.post<{ chatLink: string; status: string }>(
      `/api/admin/leads/${this.selectedLead.id}/whatsapp/link`,
      {},
      { headers: this.authHeaders() },
    ).subscribe({
      next: result => {
        this.isWhatsAppBusy = false;
        this.manualChatLink = result.chatLink;
        this.whatsAppFeedback = 'Link prepared. No message was sent; review it and choose Open WhatsApp to continue.';
        this.whatsAppFeedbackKind = 'success';
        this.refreshSelectedLead();
      },
      error: error => {
        this.isWhatsAppBusy = false;
        this.whatsAppFeedback = this.whatsAppErrorMessage(error.status, error.error?.code);
        this.whatsAppFeedbackKind = 'error';
      },
    });
  }

  sendApprovedTemplate(): void {
    if (!this.selectedLead || !this.canContactOnWhatsApp() || !this.selectedTemplate || !this.whatsAppStatus.cloudApiEnabled) return;
    this.isWhatsAppBusy = true;
    this.whatsAppFeedback = '';
    this.http.post<{ status: string; messageId: string | null }>(
      `/api/admin/leads/${this.selectedLead.id}/whatsapp/template`,
      { templateName: this.selectedTemplate, languageCode: 'en_US' },
      { headers: this.authHeaders() },
    ).subscribe({
      next: result => {
        this.isWhatsAppBusy = false;
        this.whatsAppFeedback = result.status === 'Accepted'
          ? 'Provider accepted the request. This is not a delivery confirmation.'
          : `Provider status: ${result.status}.`;
        this.whatsAppFeedbackKind = 'success';
        this.refreshSelectedLead();
      },
      error: error => {
        this.isWhatsAppBusy = false;
        this.whatsAppFeedback = this.whatsAppErrorMessage(error.status, error.error?.code);
        this.whatsAppFeedbackKind = 'error';
      },
    });
  }

  recordWhatsAppOptOut(): void {
    if (!this.selectedLead || this.selectedLead.whatsAppOptedOutAt) return;
    this.isWhatsAppBusy = true;
    this.http.post<void>(`/api/admin/leads/${this.selectedLead.id}/whatsapp/opt-out`, {}, { headers: this.authHeaders() }).subscribe({
      next: () => {
        this.isWhatsAppBusy = false;
        this.manualChatLink = '';
        this.whatsAppFeedback = 'WhatsApp opt-out recorded. Future contact via WhatsApp is blocked.';
        this.whatsAppFeedbackKind = 'success';
        this.refreshSelectedLead();
      },
      error: error => {
        this.isWhatsAppBusy = false;
        this.whatsAppFeedback = this.whatsAppErrorMessage(error.status, error.error?.code);
        this.whatsAppFeedbackKind = 'error';
      },
    });
  }

  canContactOnWhatsApp(): boolean {
    return Boolean(this.selectedLead?.whatsAppConsentGiven && !this.selectedLead.whatsAppOptedOutAt);
  }

  private loadWhatsAppStatus(): void {
    this.http.get<WhatsAppIntegrationStatus>('/api/admin/whatsapp/status', { headers: this.authHeaders() }).subscribe({
      next: status => {
        this.whatsAppStatus = status;
        this.selectedTemplate = status.approvedTemplates[0] ?? '';
      },
      error: () => this.handleRequestError(),
    });
  }

  private loadWhatsAppEvents(leadId: string): void {
    this.http.get<WhatsAppEvent[]>(`/api/admin/leads/${leadId}/whatsapp/events`, { headers: this.authHeaders() }).subscribe({
      next: events => this.whatsAppEvents = events,
      error: () => this.handleRequestError(),
    });
  }

  private refreshSelectedLead(): void {
    if (this.selectedLead) {
      this.openLead(this.selectedLead.id);
    }
  }

  private whatsAppErrorMessage(status: number, code?: string): string {
    if (code === 'WhatsAppConsentRequired') return 'Separate WhatsApp consent is required before contact.';
    if (code === 'WhatsAppOptedOut') return 'This customer opted out of WhatsApp contact.';
    if (code === 'InvalidIndianMobile') return 'The saved mobile number is not a valid Indian number.';
    if (code === 'CloudApiDisabled') return 'WhatsApp Cloud API is not configured.';
    if (code === 'TemplateNotApproved') return 'That template is not on the approved list.';
    if (status === 401 || status === 403) return 'Admin authorization is required.';
    return 'WhatsApp action failed. No delivery is confirmed.';
  }

  private authHeaders(): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${this.accessToken}` });
  }

  private handleRequestError(): void {
    this.isLoading = false;
    this.isSaving = false;
    this.errorMessage = 'Unable to load or save lead data. Your session may have expired.';
  }
}
