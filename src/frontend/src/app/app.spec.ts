import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { App } from './app';
import { routes } from './app.routes';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes)],
    })
      .compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render all six loan products and enquiry navigation', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelectorAll('[data-loan-card]').length).toBe(6);
    expect(compiled.querySelector('a[href="#enquiry"]')).toBeTruthy();
    expect(compiled.querySelector('form[aria-label="Loan enquiry form"]')).toBeTruthy();
  });

  it('should require consent before sending an enquiry', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;

    app.submitEnquiry();

    expect(app.leadForm.invalid).toBe(true);
    expect(app.feedbackKind).toBe('error');
    expect(app.feedback).toContain('consent');
  });

  it('should open the assistant and switch its conversation to Hindi', () => {
    const fixture = TestBed.createComponent(App);
    fixture.componentInstance.chatOpen = true;
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('[role="dialog"][aria-label="AI loan assistant"]')).toBeTruthy();
    expect(compiled.querySelectorAll('.quick-replies button').length).toBe(6);
    compiled.querySelector<HTMLButtonElement>('button[aria-label="हिंदी में बात करें"]')?.click();
    fixture.detectChanges();

    expect(compiled.querySelector('.chat-messages')?.textContent).toContain('लोन चाहिए');
  });

  it('should show an error when the backend lead endpoint is unavailable', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    const httpTesting = TestBed.inject(HttpTestingController);
    app.leadForm.setValue({
      fullName: 'Fictional Test Customer',
      mobileNumber: '9000000000',
      email: 'test@example.invalid',
      state: 'Sample State',
      city: 'Sample City',
      loanType: 'Personal Loan',
      requiredLoanAmount: 250000,
      monthlyIncome: 50000,
      employmentType: 'Salaried',
      consentGiven: true,
      callbackRequested: false,
      preferredContactTime: '',
      whatsAppConsentGiven: false,
    });

    app.submitEnquiry();
    const request = httpTesting.expectOne('/api/leads');
    expect(request.request.method).toBe('POST');
    request.flush({}, { status: 404, statusText: 'Not Found' });

    expect(app.feedbackKind).toBe('error');
    expect(app.feedback).toContain('could not submit');
    httpTesting.verify();
  });
});
