import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AdminDashboardComponent } from './admin-dashboard.component';

describe('AdminDashboardComponent', () => {
  let httpTesting: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminDashboardComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('does not request or render customer leads before authentication', () => {
    const fixture = TestBed.createComponent(AdminDashboardComponent);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.textContent).toContain('Admin sign in');
    expect(root.querySelector('input[type="password"]')).toBeTruthy();
    httpTesting.expectNone('/api/admin/leads');
  });

  it('logs in before fetching leads and sends filters with the admin token', () => {
    const fixture = TestBed.createComponent(AdminDashboardComponent);
    const component = fixture.componentInstance;
    component.loginForm.setValue({ username: 'admin-test', password: 'test-password' });
    component.login();

    const login = httpTesting.expectOne('/api/auth/admin/login');
    expect(login.request.method).toBe('POST');
    login.flush({ accessToken: 'test-jwt' });

    const whatsAppStatus = httpTesting.expectOne('/api/admin/whatsapp/status');
    expect(whatsAppStatus.request.headers.get('Authorization')).toBe('Bearer test-jwt');
    whatsAppStatus.flush({ cloudApiEnabled: false, webhookEnabled: false, approvedTemplates: [] });

    const firstLoad = httpTesting.expectOne('/api/admin/leads');
    expect(firstLoad.request.headers.get('Authorization')).toBe('Bearer test-jwt');
    firstLoad.flush([]);

    component.loanTypeFilter = 'Home Loan';
    component.statusFilter = 'New';
    component.searchText = 'Fictional';
    component.loadLeads();
    const filteredLoad = httpTesting.expectOne(request => request.url === '/api/admin/leads');

    expect(filteredLoad.request.params.get('loanType')).toBe('Home Loan');
    expect(filteredLoad.request.params.get('leadStatus')).toBe('New');
    expect(filteredLoad.request.params.get('search')).toBe('Fictional');
    expect(filteredLoad.request.headers.get('Authorization')).toBe('Bearer test-jwt');
    filteredLoad.flush([]);
  });

  it('sends follow-up/status updates only with the admin token', () => {
    const fixture = TestBed.createComponent(AdminDashboardComponent);
    const component = fixture.componentInstance;
    const leadId = 'c65c7e88-6188-4adb-b970-e9c7d8872f82';
    component.accessToken = 'test-jwt';
    component.selectedLead = {
      id: leadId,
      fullName: 'Fictional Customer',
      city: 'Pune',
      state: 'Maharashtra',
      loanType: 'Home Loan',
      requiredLoanAmount: 1000000,
      createdAt: '2026-11-07T10:00:00Z',
      leadStatus: 'New',
      leadSource: 'Website',
      followUpDate: null,
      whatsAppConsentGiven: false,
      whatsAppConsentAt: null,
      whatsAppOptedOutAt: null,
      lastWhatsAppActivityAt: null,
      lastWhatsAppStatus: null,
      score: 70,
      priority: 'High',
      mobileNumber: '9876543210',
      email: null,
      monthlyIncome: 50000,
      employmentType: 'Salaried',
      consentGiven: true,
      callbackRequested: true,
      preferredContactTime: 'Morning',
      followUpNotes: null,
      scoreSignals: [],
      aiSummary: 'Home Loan enquiry.',
    };
    component.managementForm.setValue({
      leadStatus: 'Contacted',
      followUpDate: '2026-11-20',
      followUpNotes: 'Fictional follow-up',
    });

    component.saveManagement();
    const update = httpTesting.expectOne(`/api/admin/leads/${leadId}/management`);
    expect(update.request.method).toBe('PUT');
    expect(update.request.headers.get('Authorization')).toBe('Bearer test-jwt');
    expect(update.request.body).toMatchObject({ leadStatus: 'Contacted', followUpDate: '2026-11-20' });
    update.flush(null);
    httpTesting.expectOne('/api/admin/leads').flush([component.selectedLead]);
    httpTesting.expectOne(`/api/admin/leads/${leadId}`).flush(component.selectedLead);
    httpTesting.expectOne(`/api/admin/leads/${leadId}/whatsapp/events`).flush([]);
  });

  it('does not prepare a WhatsApp link without separate consent', () => {
    const fixture = TestBed.createComponent(AdminDashboardComponent);
    const component = fixture.componentInstance;
    component.accessToken = 'test-jwt';
    component.selectedLead = {
      id: 'b0113f24-8b5e-45c4-a472-66f9a77d8dbb',
      fullName: 'Fictional Customer',
      city: 'Pune',
      state: 'Maharashtra',
      loanType: 'Home Loan',
      requiredLoanAmount: 1000000,
      createdAt: '2026-11-07T10:00:00Z',
      leadStatus: 'New',
      leadSource: 'Website',
      followUpDate: null,
      score: 70,
      priority: 'High',
      mobileNumber: '9876543210',
      email: null,
      monthlyIncome: 50000,
      employmentType: 'Salaried',
      consentGiven: true,
      callbackRequested: true,
      preferredContactTime: 'Morning',
      followUpNotes: null,
      scoreSignals: [],
      aiSummary: 'Home Loan enquiry.',
      whatsAppConsentGiven: false,
      whatsAppConsentAt: null,
      whatsAppOptedOutAt: null,
      lastWhatsAppActivityAt: null,
      lastWhatsAppStatus: null,
    };

    component.prepareWhatsAppLink();

    expect(component.canContactOnWhatsApp()).toBe(false);
    httpTesting.expectNone(request => request.url.includes('/whatsapp/link'));
  });

  it('prepares a manual link after separate consent without opening or sending it', () => {
    const fixture = TestBed.createComponent(AdminDashboardComponent);
    const component = fixture.componentInstance;
    const leadId = 'c0d0e949-78f5-4bdd-9817-65a5d19b289e';
    component.accessToken = 'test-jwt';
    component.selectedLead = {
      id: leadId,
      fullName: 'Fictional Customer',
      city: 'Pune',
      state: 'Maharashtra',
      loanType: 'Personal Loan',
      requiredLoanAmount: 300000,
      createdAt: '2026-11-07T10:00:00Z',
      leadStatus: 'New',
      leadSource: 'Website',
      followUpDate: null,
      score: 85,
      priority: 'High',
      mobileNumber: '9876543210',
      email: null,
      monthlyIncome: 70000,
      employmentType: 'Salaried',
      consentGiven: true,
      callbackRequested: true,
      preferredContactTime: 'Evening',
      followUpNotes: null,
      scoreSignals: [],
      aiSummary: 'Personal Loan enquiry.',
      whatsAppConsentGiven: true,
      whatsAppConsentAt: '2026-11-07T10:00:00Z',
      whatsAppOptedOutAt: null,
      lastWhatsAppActivityAt: null,
      lastWhatsAppStatus: null,
    };
    const openSpy = vi.spyOn(window, 'open');

    component.prepareWhatsAppLink();
    const request = httpTesting.expectOne(`/api/admin/leads/${leadId}/whatsapp/link`);
    expect(request.request.headers.get('Authorization')).toBe('Bearer test-jwt');
    request.flush({ success: true, chatLink: 'https://wa.me/919876543210?text=sample', status: 'LinkReadyNotSent' });
    expect(component.manualChatLink).toContain('wa.me/919876543210');
    expect(openSpy).not.toHaveBeenCalled();

    const detail = httpTesting.expectOne(`/api/admin/leads/${leadId}`);
    detail.flush(component.selectedLead);
    httpTesting.expectOne(`/api/admin/leads/${leadId}/whatsapp/events`).flush([]);
    openSpy.mockRestore();
  });
});
