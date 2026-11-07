import { Component } from '@angular/core';
import { Routes } from '@angular/router';
import { TrustPageComponent, TrustPageContent } from './trust-page.component';

const pages: Record<string, TrustPageContent> = {
	privacy: {
		eyebrow: 'Your information',
		title: 'Privacy Policy',
		intro: 'We collect only the information needed to understand a loan enquiry and support a customer-requested follow-up.',
		sections: [
			{
				title: 'Information collected',
				paragraphs: ['A customer may provide name, mobile number, optional email, city, state, loan type, requested amount, monthly income, employment type, callback preference and contact times. Enquiry and WhatsApp communication consents are recorded separately.'],
				bullets: ['We do not request Aadhaar, PAN, OTPs, bank PINs, card details or bank statements in the initial enquiry.', 'The AI assistant conversation stays in the browser until a customer chooses to submit an enquiry.'],
			},
			{
				title: 'Use and sharing',
				paragraphs: ['Information is used to respond to the enquiry, manage requested follow-up and maintain consent and communication records. Customer information is shared only with appropriate lending or service parties for the stated purpose and with valid consent.'],
			},
			{
				title: 'Choices and retention',
				paragraphs: ['WhatsApp follow-up is optional and has a separate consent choice. Customers may opt out using STOP or by asking the team to record an opt-out. Enquiry records are retained only as needed for service, security and applicable legal obligations.'],
			},
			{
				title: 'Security and requests',
				paragraphs: ['Lead details are restricted to authenticated administrators. No internet service can promise absolute security. Before public launch, the operator must configure a verified support contact for access, correction, deletion and privacy requests.'],
			},
		],
	},
	terms: {
		eyebrow: 'Using the platform',
		title: 'Terms and Conditions',
		intro: 'IndiaFinance AI helps customers share loan enquiries and explore general information. Submitting an enquiry is not a loan application or approval.',
		sections: [
			{ title: 'Platform role', paragraphs: ['IndiaFinance AI is a loan assistance and lead generation platform, not a bank or NBFC unless separately authorised. It does not lend money or make lender decisions.'] },
			{ title: 'Enquiries and lender decisions', paragraphs: ['Customers should submit accurate information they are authorised to share. A lender may request additional information through its own process. Approval, interest rates, fees, repayment terms and eligibility are determined solely by the lender.'] },
			{ title: 'No guarantee', paragraphs: ['No enquiry, conversation, score, summary or follow-up guarantees a loan offer, approval, rate or disbursement. Review the lender’s official offer and agreement before accepting any product.'] },
			{ title: 'Acceptable use', paragraphs: ['Do not submit another person’s information without authority, attempt to access admin functions, or use the service for unlawful or deceptive activity. Contact choices and opt-outs must be respected.'] },
		],
	},
	disclaimer: {
		eyebrow: 'Important information',
		title: 'Loan Disclaimer',
		intro: 'Loan information on this platform is general and indicative. It is not a credit decision, financial advice or an offer to lend.',
		sections: [
			{ title: 'No bank or NBFC representation', paragraphs: ['IndiaFinance AI is a loan assistance and lead generation platform, not a bank or NBFC unless separately authorised. Any lender relationship or authorisation must be stated specifically and accurately.'] },
			{ title: 'Approval and eligibility', paragraphs: ['Loan approval is subject to lender assessment, verification, policy, documentation and applicable law. Eligibility is not guaranteed by a calculator, assistant, score or representative.'] },
			{ title: 'Rates and charges', paragraphs: ['Interest rates, processing fees, penalties and all other charges depend on the lender and the final agreement. Do not rely on an estimated figure as a quote or promise.'] },
			{ title: 'Customer information', paragraphs: ['Information is shared only with appropriate parties for the stated enquiry purpose and with valid consent. Do not send Aadhaar, PAN, OTPs, passwords, bank PINs or card details through this platform.'] },
		],
	},
	contact: {
		eyebrow: 'Get in touch',
		title: 'Contact Us',
		intro: 'For a loan enquiry, use the enquiry form and select whether you would like a callback. WhatsApp contact is optional and requires separate consent.',
		sections: [
			{ title: 'Loan enquiry', paragraphs: ['Use the form on the homepage to share the minimum details needed for a loan enquiry. You can request a callback without agreeing to WhatsApp messages.'] },
			{ title: 'Privacy or platform support', paragraphs: ['A verified support email, registered business address and complaint contact have not been supplied in this project. The operator must configure and publish those official contact details before public launch; do not rely on an invented contact address.'] },
		],
	},
	about: {
		eyebrow: 'About the platform',
		title: 'About IndiaFinance AI',
		intro: 'IndiaFinance AI is designed to help customers understand loan categories, share an enquiry with consent and request a follow-up.',
		sections: [
			{ title: 'What we do', paragraphs: ['The platform provides general information and helps route customer enquiries to appropriate lending parties when the customer gives valid consent. It is not a lender.'] },
			{ title: 'What we do not promise', paragraphs: ['We do not guarantee loan approval, eligibility, interest rates, fees or disbursement. Final terms and decisions belong to the lender. IndiaFinance AI is not a bank or NBFC unless separately authorised.'] },
			{ title: 'Customer-first contact', paragraphs: ['Customers choose whether to request a callback and whether to opt in separately to WhatsApp follow-up. Unsolicited promotional messages are not sent by this application.'] },
		],
	},
};

@Component({ standalone: true, template: '' })
class HomeRouteComponent {}

export const routes: Routes = [
	{ path: '', pathMatch: 'full', component: HomeRouteComponent },
	...Object.entries(pages).map(([path, page]) => ({
		path,
		component: TrustPageComponent,
		data: { page },
	})),
	{ path: '**', redirectTo: '/' },
];
