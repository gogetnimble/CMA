import { expect } from 'chai';
import { MSCForm, AccountBaseForm } from '../src/Account.js';
import { createMockFormContext, createMockExecutionContext } from './mockExecutionContext.js';

describe('MSCForm', () => {
    let logMessages;

    beforeEach(() => {
        logMessages = [];
        global.console = { log: (msg) => logMessages.push(msg) };
    });

    function buildFormContext(accountTypeName = null) {
        return createMockFormContext(
            {
                telephone1: '6133254463',
                new_accounttypeid: accountTypeName
                    ? [{ id: 'type-guid', name: accountTypeName }]
                    : null,
            },
            {
                tabs: {
                    SUMMARY_TAB: ['university_details_section'],
                },
            }
        );
    }

    it('initializes without throwing', () => {
        const form = new MSCForm(buildFormContext());
        expect(() => form.initialize()).to.not.throw();
    });

    it('is an instance of AccountBaseForm', () => {
        expect(new MSCForm(buildFormContext())).to.be.instanceOf(AccountBaseForm);
    });

    it('logs the MSC Form initialization message', () => {
        const form = new MSCForm(buildFormContext());
        form.initialize();
        const msg = logMessages.find((m) => m.includes('MSC Form initializing'));
        expect(msg).to.not.be.undefined;
    });

    it('wires onChange handler for new_accounttypeid', () => {
        const formContext = buildFormContext();
        const form = new MSCForm(formContext);
        form.initialize();

        let fired = false;
        formContext.getAttribute('new_accounttypeid').addOnChange(() => { fired = true; });
        formContext.getAttribute('new_accounttypeid').fireOnChange();
        expect(fired).to.be.true;
    });

    it('fires onChange for new_accounttypeid on initialize to set initial section state', () => {
        const formContext = buildFormContext('University / Université');
        const form = new MSCForm(formContext);
        form.initialize();

        const section = formContext.ui.tabs
            .get('SUMMARY_TAB')
            .sections.get('university_details_section');
        expect(section.getVisible()).to.be.true;
    });

    // ─── OnChange_ToggleUniversityDetails ──────────────────────────────────────────

    describe('OnChange_ToggleUniversityDetails()', () => {
        it('shows the university_details_section when account type is "University / Université"', () => {
            const formContext = buildFormContext('University / Université');
            const form = new MSCForm(formContext);
            const ctx = { getFormContext: () => formContext };

            form.OnChange_ToggleUniversityDetails(ctx);

            const section = formContext.ui.tabs
                .get('SUMMARY_TAB')
                .sections.get('university_details_section');
            expect(section.getVisible()).to.be.true;
        });

        it('hides the university_details_section for any other account type', () => {
            const formContext = buildFormContext('Corporate');
            const section = formContext.ui.tabs
                .get('SUMMARY_TAB')
                .sections.get('university_details_section');
            section.setVisible(true); // ensure it starts visible

            const form = new MSCForm(formContext);
            const ctx = { getFormContext: () => formContext };
            form.OnChange_ToggleUniversityDetails(ctx);

            expect(section.getVisible()).to.be.false;
        });

        it('hides the section when account type is null', () => {
            const formContext = buildFormContext(null);
            const section = formContext.ui.tabs
                .get('SUMMARY_TAB')
                .sections.get('university_details_section');
            section.setVisible(true);

            const form = new MSCForm(formContext);
            const ctx = { getFormContext: () => formContext };
            form.OnChange_ToggleUniversityDetails(ctx);

            expect(section.getVisible()).to.be.false;
        });

        it('does not throw when SUMMARY_TAB is absent', () => {
            const formContext = createMockFormContext(
                { new_accounttypeid: [{ id: 'x', name: 'University / Université' }] },
                {} // no tabs defined
            );
            const form = new MSCForm(formContext);
            const ctx = { getFormContext: () => formContext };
            expect(() => form.OnChange_ToggleUniversityDetails(ctx)).to.not.throw();
        });
    });
});
