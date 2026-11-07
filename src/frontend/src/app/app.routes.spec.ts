import { routes } from './app.routes';
import { TrustPageContent } from './trust-page.component';

describe('customer trust routes', () => {
  it('exposes privacy, terms, disclaimer, contact, and about pages with required disclosures', () => {
    const requiredPaths = ['privacy', 'terms', 'disclaimer', 'contact', 'about'];
    const trustRoutes = routes.filter(route => requiredPaths.includes(route.path ?? ''));
    expect(trustRoutes.map(route => route.path)).toEqual(requiredPaths);

    const contents = trustRoutes.map(route => route.data?.['page'] as TrustPageContent);
    const allText = contents.flatMap(page => [page.intro, ...page.sections.flatMap(section => section.paragraphs)]).join(' ').toLowerCase();
    expect(allText).toContain('not a bank or nbfc');
    expect(allText).toContain('subject to lender assessment');
    expect(allText).toContain('interest rates, fees');
    expect(allText).toContain('valid consent');
  });

  it('does not invent contact coordinates', () => {
    const contactRoute = routes.find(route => route.path === 'contact');
    const page = contactRoute?.data?.['page'] as TrustPageContent;
    const contactText = page.sections.flatMap(section => section.paragraphs).join(' ').toLowerCase();

    expect(contactText).toContain('have not been supplied');
    expect(contactText).not.toContain('@indiafinance');
  });
});
