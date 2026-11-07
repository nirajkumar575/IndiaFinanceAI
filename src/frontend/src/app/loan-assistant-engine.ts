import { inject, Injectable, InjectionToken } from '@angular/core';

export type AssistantLocale = 'en' | 'hi';
export type AssistantStep =
  | 'loanType'
  | 'amount'
  | 'income'
  | 'employment'
  | 'location'
  | 'contact'
  | 'name'
  | 'mobile'
  | 'preferredTime'
  | 'consent'
  | 'whatsAppConsent'
  | 'done';

export interface AssistantLeadDraft {
  loanType?: string;
  requiredLoanAmount?: number;
  monthlyIncome?: number;
  employmentType?: string;
  city?: string;
  state?: string;
  fullName?: string;
  mobileNumber?: string;
  callbackRequested?: boolean;
  preferredContactTime?: string;
  whatsAppConsentGiven?: boolean;
}

export interface AssistantState {
  step: AssistantStep;
  lead: AssistantLeadDraft;
}

export interface AssistantTurn {
  reply: string;
  state: AssistantState;
  submitLead?: boolean;
}

export interface LoanAssistantEngine {
  greeting(locale: AssistantLocale): string;
  initialState(): AssistantState;
  currentQuestion(state: AssistantState, locale: AssistantLocale): string;
  respond(message: string, state: AssistantState, locale: AssistantLocale): AssistantTurn;
}

export const LOAN_ASSISTANT_ENGINE = new InjectionToken<LoanAssistantEngine>(
  'LOAN_ASSISTANT_ENGINE',
  {
    providedIn: 'root',
    factory: () => inject(RuleBasedLoanAssistantEngine),
  },
);

@Injectable({ providedIn: 'root' })
export class RuleBasedLoanAssistantEngine implements LoanAssistantEngine {
  private readonly products = [
    'Personal Loan',
    'Business Loan',
    'MSME Loan',
    'Home Loan',
    'Vehicle Loan',
    'Loan Against Property',
  ];

  greeting(locale: AssistantLocale): string {
    return locale === 'hi'
      ? 'नमस्ते! मैं IndiaFinance AI सहायक हूँ। मैं सामान्य जानकारी दे सकता हूँ और आपकी सहमति से टीम तक enquiry पहुँचा सकता हूँ। किस प्रकार का लोन चाहिए?'
      : 'Hello! I am the IndiaFinance AI assistant. I can share general information and pass your enquiry to our team with your consent. Which loan do you need?';
  }

  initialState(): AssistantState {
    return { step: 'loanType', lead: {} };
  }

  currentQuestion(state: AssistantState, locale: AssistantLocale): string {
    return this.promptFor(state.step, locale);
  }

  respond(message: string, state: AssistantState, locale: AssistantLocale): AssistantTurn {
    const text = message.trim();
    const normalized = text.toLocaleLowerCase();

    if (this.containsSensitiveRequest(normalized)) {
      return this.turn(state, locale === 'hi'
        ? 'कृपया OTP, पासवर्ड, बैंक PIN, कार्ड विवरण, Aadhaar या PAN यहाँ साझा न करें। मैं इन्हें नहीं माँगूँगा।'
        : 'Please do not share an OTP, password, bank PIN, card details, Aadhaar or PAN here. I will never ask for them.', this.promptFor(state.step, locale));
    }

    const knowledge = this.knowledgeReply(normalized, locale);
    if (knowledge) {
      return this.turn(state, knowledge, this.promptFor(state.step, locale));
    }

    switch (state.step) {
      case 'loanType': {
        const loanType = this.matchLoanType(normalized);
        if (!loanType) {
          return this.turn(state, locale === 'hi'
            ? 'कृपया सूची में से एक लोन प्रकार चुनें।'
            : 'Please choose one of the listed loan types.', this.promptFor(state.step, locale));
        }
        const next = { ...state.lead, loanType };
        return this.turn({ step: 'amount', lead: next }, locale === 'hi'
          ? `${loanType} के बारे में जानकारी दे सकता हूँ। आपको कितनी राशि चाहिए?`
          : `I can help with general information about a ${loanType}. What loan amount do you require?`);
      }
      case 'amount': {
        const amount = this.parseAmount(text);
        if (!amount || amount < 10000 || amount > 50000000) {
          return this.turn(state, locale === 'hi'
            ? 'कृपया ₹10,000 से ₹5 करोड़ के बीच की राशि लिखें।'
            : 'Please enter an amount between ₹10,000 and ₹5 crore.', this.promptFor(state.step, locale));
        }
        return this.turn({ step: 'income', lead: { ...state.lead, requiredLoanAmount: amount } }, this.promptFor('income', locale));
      }
      case 'income': {
        const income = this.parseAmount(text);
        if (!income || income < 1 || income > 100000000) {
          return this.turn(state, locale === 'hi'
            ? 'कृपया अपनी मासिक आय रुपये में लिखें।'
            : 'Please enter your monthly income in rupees.', this.promptFor(state.step, locale));
        }
        return this.turn({ step: 'employment', lead: { ...state.lead, monthlyIncome: income } }, this.promptFor('employment', locale));
      }
      case 'employment': {
        const employmentType = this.matchEmploymentType(normalized);
        if (!employmentType) {
          return this.turn(state, locale === 'hi'
            ? 'कृपया बताएं कि आप वेतनभोगी हैं या स्वरोजगार करते हैं।'
            : 'Please say whether you are salaried or self-employed.', this.promptFor(state.step, locale));
        }
        return this.turn({ step: 'location', lead: { ...state.lead, employmentType } }, this.promptFor('location', locale));
      }
      case 'location': {
        const parts = text.split(',').map(part => part.trim()).filter(Boolean);
        if (parts.length < 2 || parts.some(part => part.length > 100)) {
          return this.turn(state, locale === 'hi'
            ? 'कृपया शहर और राज्य comma से अलग करके लिखें, जैसे: पुणे, महाराष्ट्र।'
            : 'Please enter your city and state separated by a comma, such as Pune, Maharashtra.', this.promptFor(state.step, locale));
        }
        return this.turn({ step: 'contact', lead: { ...state.lead, city: parts[0], state: parts.slice(1).join(', ') } }, this.promptFor('contact', locale));
      }
      case 'contact': {
        if (this.isNo(normalized)) {
          return this.turn({ ...state, step: 'done' }, locale === 'hi'
            ? 'ठीक है। आपकी जानकारी कहीं भेजी या सेव नहीं की गई। मदद के लिए आप फिर से chat शुरू कर सकते हैं।'
            : 'Understood. Your information has not been sent or saved. You can start a new chat if you need help.');
        }
        if (!this.isYes(normalized)) {
          return this.turn(state, locale === 'hi'
            ? 'कृपया “हाँ” या “नहीं” में बताएं।'
            : 'Please answer yes or no.', this.promptFor(state.step, locale));
        }
        return this.turn({ step: 'name', lead: { ...state.lead, callbackRequested: true } }, locale === 'hi'
          ? 'आपकी टीम से बात कराने के लिए आपका पूरा नाम क्या है?'
          : 'What is your full name so our team can follow up?');
      }
      case 'name': {
        if (text.length < 2 || text.length > 100) {
          return this.turn(state, locale === 'hi' ? 'कृपया अपना नाम लिखें।' : 'Please enter your name.');
        }
        return this.turn({ step: 'mobile', lead: { ...state.lead, fullName: text } }, locale === 'hi'
          ? 'कौन-से 10-अंकों के मोबाइल नंबर पर संपर्क करें?'
          : 'Which 10-digit mobile number may the team use to contact you?');
      }
      case 'mobile': {
        const digits = text.replace(/\D/g, '');
        const mobileNumber = digits.length === 12 && digits.startsWith('91') ? digits.slice(2) : digits;
        if (!/^[6-9][0-9]{9}$/.test(mobileNumber)) {
          return this.turn(state, locale === 'hi'
            ? 'कृपया 6 से शुरू होने वाला सही 10-अंकों का भारतीय मोबाइल नंबर लिखें।'
            : 'Please enter a valid 10-digit Indian mobile number starting with 6, 7, 8 or 9.');
        }
        return this.turn({ step: 'preferredTime', lead: { ...state.lead, mobileNumber } }, this.promptFor('preferredTime', locale));
      }
      case 'preferredTime': {
        const preferred = this.matchPreferredTime(normalized);
        if (!preferred && !/^(any time|anytime|कोई भी समय|कोई समय नहीं)$/i.test(normalized)) {
          return this.turn(state, locale === 'hi'
            ? 'सुबह, दोपहर, शाम या “कोई भी समय” चुनें।'
            : 'Choose morning, afternoon, evening or “any time”.', this.promptFor(state.step, locale));
        }
        return this.turn({ step: 'consent', lead: { ...state.lead, preferredContactTime: preferred } }, locale === 'hi'
          ? 'क्या आप सहमत हैं कि IndiaFinance AI इस enquiry के बारे में आपसे संपर्क करे? सहमति के बिना कुछ भी भेजा नहीं जाएगा।'
          : 'Do you consent to IndiaFinance AI contacting you about this enquiry? Nothing will be sent without your consent.');
      }
      case 'consent': {
        if (this.isYes(normalized)) {
          return this.turn({ ...state, step: 'whatsAppConsent' }, locale === 'hi'
            ? 'क्या आप इस enquiry के follow-up के लिए अलग से WhatsApp संदेश स्वीकार करते हैं? यह optional है।'
            : 'Separately, do you consent to receiving WhatsApp messages about this enquiry? This is optional.');
        }
        if (this.isNo(normalized)) {
          return this.turn({ ...state, step: 'done' }, locale === 'hi'
            ? 'कोई बात नहीं। आपकी enquiry भेजी या सेव नहीं की गई।'
            : 'No problem. Your enquiry has not been sent or saved.');
        }
        return this.turn(state, locale === 'hi' ? 'कृपया “हाँ” या “नहीं” में बताएं।' : 'Please answer yes or no.', this.promptFor(state.step, locale));
      }
      case 'whatsAppConsent': {
        if (!this.isYes(normalized) && !this.isNo(normalized)) {
          return this.turn(state, locale === 'hi' ? 'कृपया WhatsApp consent के लिए “हाँ” या “नहीं” में जवाब दें।' : 'Please answer yes or no for WhatsApp consent.', this.promptFor(state.step, locale));
        }
        return {
          reply: locale === 'hi' ? 'धन्यवाद। आपकी enquiry भेज रहा हूँ…' : 'Thank you. Sending your enquiry…',
          state: { step: 'done', lead: { ...state.lead, whatsAppConsentGiven: this.isYes(normalized) } },
          submitLead: true,
        };
      }
      case 'done':
        return this.turn(state, locale === 'hi'
          ? 'यह बातचीत पूरी हो गई है। नई बातचीत के लिए “नई chat” चुनें।'
          : 'This conversation is complete. Choose “New chat” to begin again.');
    }
  }

  private promptFor(step: AssistantStep, locale: AssistantLocale): string {
    if (locale === 'hi') {
      const prompts: Record<AssistantStep, string> = {
        loanType: 'आपको किस प्रकार का लोन चाहिए?',
        amount: 'आपको कितनी राशि चाहिए? रुपये में लिखें।',
        income: 'आपकी मासिक आय कितनी है? रुपये में लिखें।',
        employment: 'आप वेतनभोगी हैं या स्वरोजगार करते हैं?',
        location: 'आप किस शहर और राज्य से हैं? “शहर, राज्य” लिखें।',
        contact: 'क्या आप चाहेंगे कि हमारी टीम आपसे संपर्क करे?',
        name: 'आपका पूरा नाम क्या है?',
        mobile: 'किस मोबाइल नंबर पर संपर्क करें?',
        preferredTime: 'आपको संपर्क करने का कौन-सा समय सुविधाजनक है?',
        consent: 'क्या आप enquiry के लिए संपर्क की सहमति देते हैं?',
        whatsAppConsent: 'क्या आप enquiry के follow-up के लिए WhatsApp संदेशों की अलग सहमति देते हैं? (हाँ/नहीं)',
        done: '',
      };
      return prompts[step];
    }

    const prompts: Record<AssistantStep, string> = {
      loanType: 'Which loan do you need?',
      amount: 'What loan amount do you require? Please enter rupees.',
      income: 'What is your monthly income? Please enter rupees.',
      employment: 'Are you salaried or self-employed?',
      location: 'Which city and state are you from? Enter “City, State”.',
      contact: 'Would you like our team to contact you?',
      name: 'What is your full name?',
      mobile: 'Which mobile number may the team use?',
      preferredTime: 'What time is best for a callback: morning, afternoon or evening?',
      consent: 'Do you consent to be contacted about this enquiry?',
      whatsAppConsent: 'Do you separately consent to WhatsApp follow-up messages about this enquiry? (Yes/No)',
      done: '',
    };
    return prompts[step];
  }

  private knowledgeReply(message: string, locale: AssistantLocale): string | undefined {
    const isHindi = locale === 'hi';
    if (/otp|password|passcode|bank pin|\bpin\b|card detail|aadhaar|aadhar|\bpan\b|बैंक पिन|पासवर्ड|ओटीपी|आधार|पैन/.test(message)) {
      return isHindi
        ? 'सुरक्षा के लिए OTP, पासवर्ड, PIN, कार्ड विवरण, Aadhaar या PAN कभी भी chat में साझा न करें।'
        : 'For your security, never share an OTP, password, PIN, card details, Aadhaar or PAN in chat.';
    }
    if (/emi|ईएमआई/.test(message)) {
      return isHindi
        ? 'EMI वह मासिक किस्त है जिसमें मूलधन और ब्याज शामिल होते हैं। सही EMI राशि, अवधि और lender द्वारा तय ब्याज दर पर निर्भर करती है; मैं किसी lender की दर का अनुमान नहीं लगाता।'
        : 'An EMI is a monthly repayment that includes principal and interest. The exact EMI depends on the amount, tenure and the lender’s rate; I do not guess lender rates.';
    }
    if (/eligib|eligible|पात्र|योग्य/.test(message)) {
      return isHindi
        ? 'पात्रता lender की policy, आय, repayment क्षमता और दस्तावेज़ों पर निर्भर करती है। यह सामान्य जानकारी है, पात्रता या approval की guarantee नहीं। अंतिम निर्णय lender लेता है।'
        : 'Eligibility depends on the lender’s policy, income, repayment capacity and documents. This is general information, not an eligibility or approval guarantee. The lender makes the final decision.';
    }
    if (/interest rate|\brate\b|ब्याज दर|गारंटी|guarantee|approve|approval|मंजूर/.test(message)) {
      return isHindi
        ? 'ब्याज दर और approval lender तय करता है। मेरे पास आपकी enquiry के लिए सत्यापित lender offer नहीं है, इसलिए मैं rate या approval का वादा नहीं कर सकता।'
        : 'The lender determines rates and approval. I do not have a verified lender offer for your enquiry, so I cannot promise a rate or approval.';
    }

    const loanType = this.matchLoanType(message);
    const asksAboutProduct = /\?|\b(what|tell|about|explain|info|details)\b|क्या|बताइ|जानकारी/.test(message);
    if (loanType && asksAboutProduct) {
      const descriptions: Record<string, [string, string]> = {
        'Personal Loan': ['व्यक्तिगत जरूरतों के लिए unsecured विकल्प हो सकता है; शर्तें lender तय करता है।', 'May be an unsecured option for personal needs; terms are set by the lender.'],
        'Business Loan': ['व्यवसाय की जरूरतों के लिए financing विकल्प हो सकता है; स्वीकृति lender की जाँच पर निर्भर है।', 'May be a financing option for business needs; approval depends on lender review.'],
        'MSME Loan': ['पात्र छोटे व्यवसायों के लिए विकल्प हो सकता है; उपलब्धता और शर्तें lender पर निर्भर हैं।', 'May be an option for eligible small businesses; availability and terms depend on the lender.'],
        'Home Loan': ['घर खरीदने या निर्माण के लिए financing विकल्प हो सकता है; lender की शर्तें लागू होंगी।', 'May help finance a home purchase or construction, subject to lender terms.'],
        'Vehicle Loan': ['नए या pre-owned वाहन के लिए financing विकल्प हो सकता है; अंतिम शर्तें lender तय करता है।', 'May help finance a new or pre-owned vehicle; final terms are set by the lender.'],
        'Loan Against Property': ['पात्र property के बदले secured loan हो सकता है; lender property और repayment क्षमता का मूल्यांकन करता है।', 'May be a secured loan against eligible property; the lender assesses the property and repayment capacity.'],
      };
      const description = descriptions[loanType];
      return description[isHindi ? 0 : 1];
    }
    return undefined;
  }

  private matchLoanType(message: string): string | undefined {
    if (/loan against property|\blap\b|संपत्ति के बदले|सम्पत्ति के बदले/.test(message)) return this.products[5];
    if (/msme|एमएसएमई|small business loan/.test(message)) return this.products[2];
    if (/business|व्यवसाय|कारोबार/.test(message)) return this.products[1];
    if (/home loan|housing|गृह ऋण|होम लोन/.test(message)) return this.products[3];
    if (/vehicle|car loan|auto loan|वाहन|गाड़ी|गाड़ी/.test(message)) return this.products[4];
    if (/personal loan|व्यक्तिगत ऋण|पर्सनल लोन/.test(message)) return this.products[0];
    return undefined;
  }

  private matchEmploymentType(message: string): string | undefined {
    if (/self.?employed|स्वरोजगार|स्वयं.?रोजगार|अपना व्यवसाय|अपना कारोबार/.test(message)) return 'Self-employed';
    if (/salaried|salary|वेतनभोगी|नौकरी|सैलरी/.test(message)) return 'Salaried';
    return undefined;
  }

  private matchPreferredTime(message: string): string | undefined {
    if (/morning|सुबह/.test(message)) return 'Morning';
    if (/afternoon|दोपहर/.test(message)) return 'Afternoon';
    if (/evening|शाम/.test(message)) return 'Evening';
    return undefined;
  }

  private parseAmount(text: string): number | undefined {
    const normalized = text.toLocaleLowerCase().replace(/₹|rs\.?|inr|रुपये|रुपया/g, '').trim();
    const match = normalized.match(/[0-9][0-9,]*(?:\.[0-9]+)?/);
    if (!match) return undefined;
    const value = Number(match[0].replaceAll(',', ''));
    if (!Number.isFinite(value)) return undefined;
    if (/crore|करोड़|करोड/.test(normalized)) return Math.round(value * 10000000);
    if (/lakh|lac|लाख/.test(normalized)) return Math.round(value * 100000);
    if (/\bk\b|thousand|हजार/.test(normalized)) return Math.round(value * 1000);
    return Math.round(value);
  }

  private containsSensitiveRequest(message: string): boolean {
    return /otp|password|passcode|bank pin|\bpin\b|card detail|aadhaar|aadhar|\bpan\b|बैंक पिन|पासवर्ड|ओटीपी|आधार|पैन/.test(message);
  }

  private isYes(message: string): boolean {
    return /^(yes|y|yeah|sure|haan|han|हाँ|हां|जी|ठीक है)$/i.test(message);
  }

  private isNo(message: string): boolean {
    return /^(no|n|nope|nah|nahi|नहीं|नही|ना)$/i.test(message);
  }

  private turn(state: AssistantState, leadIn: string, prompt?: string): AssistantTurn {
    return { reply: prompt ? `${leadIn}\n${prompt}` : leadIn, state };
  }
}
