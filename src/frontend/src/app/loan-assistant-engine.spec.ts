import { RuleBasedLoanAssistantEngine } from './loan-assistant-engine';

describe('RuleBasedLoanAssistantEngine', () => {
  const engine = new RuleBasedLoanAssistantEngine();

  it('should guide a customer through a complete consented enquiry', () => {
    let state = engine.initialState();
    const answer = (message: string) => {
      const turn = engine.respond(message, state, 'en');
      state = turn.state;
      return turn;
    };

    expect(engine.greeting('en')).toContain('Which loan');
    answer('Personal Loan');
    expect(state.step).toBe('amount');
    answer('5 lakh');
    expect(state.lead.requiredLoanAmount).toBe(500000);
    answer('50000');
    expect(state.lead.monthlyIncome).toBe(50000);
    answer('Salaried');
    answer('Pune, Maharashtra');
    expect(state.lead.city).toBe('Pune');
    expect(state.lead.state).toBe('Maharashtra');
    answer('Yes');
    answer('Fictional Customer');
    answer('9876543210');
    answer('Evening');
    const contactConsent = answer('Yes');
    expect(contactConsent.submitLead).toBeUndefined();
    const consent = answer('Yes');

    expect(consent.submitLead).toBe(true);
    expect(state.step).toBe('done');
    expect(state.lead).toMatchObject({
      loanType: 'Personal Loan',
      requiredLoanAmount: 500000,
      monthlyIncome: 50000,
      employmentType: 'Salaried',
      city: 'Pune',
      state: 'Maharashtra',
      fullName: 'Fictional Customer',
      mobileNumber: '9876543210',
      callbackRequested: true,
      preferredContactTime: 'Evening',
      whatsAppConsentGiven: true,
    });
  });

  it('should support Hindi prompts and stop when contact is declined', () => {
    let state = engine.initialState();
    expect(engine.greeting('hi')).toContain('नमस्ते');
    state = engine.respond('Personal Loan', state, 'hi').state;
    state = engine.respond('1 लाख', state, 'hi').state;
    state = engine.respond('50000', state, 'hi').state;
    state = engine.respond('वेतनभोगी', state, 'hi').state;
    state = engine.respond('जयपुर, राजस्थान', state, 'hi').state;
    const turn = engine.respond('नहीं', state, 'hi');

    expect(turn.submitLead).toBeUndefined();
    expect(turn.state.step).toBe('done');
    expect(turn.reply).toContain('सेव नहीं');
  });

  it('should submit an enquiry when WhatsApp opt-in is declined without granting channel consent', () => {
    let state = engine.initialState();
    const answer = (message: string) => {
      const turn = engine.respond(message, state, 'en');
      state = turn.state;
      return turn;
    };

    answer('Personal Loan');
    answer('2 lakh');
    answer('50000');
    answer('Salaried');
    answer('Pune, Maharashtra');
    answer('Yes');
    answer('Fictional Customer');
    answer('9876543210');
    answer('Morning');
    const enquiryConsent = answer('Yes');
    expect(enquiryConsent.submitLead).toBeUndefined();

    const whatsappConsent = answer('No');

    expect(whatsappConsent.submitLead).toBe(true);
    expect(state.lead.whatsAppConsentGiven).toBe(false);
  });

  it('should answer EMI questions without inventing rates and block sensitive details', () => {
    const state = engine.initialState();
    const emi = engine.respond('What is the EMI?', state, 'en');
    const sensitive = engine.respond('My OTP is 123456', state, 'en');

    expect(emi.reply).toContain('I do not guess lender rates');
    expect(emi.state).toEqual(state);
    expect(sensitive.reply).toContain('do not share an OTP');
    expect(sensitive.state).toEqual(state);
  });
});
