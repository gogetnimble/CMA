import { expect } from 'chai';
import { CMAConnectForm, AccountBaseForm } from '../src/Account.js';
import { createMockFormContext, createMockExecutionContext, createMockXrm } from './mockExecutionContext.js';

describe('Account CMAConnectForm Tests', () => {
    let formContext;
    let form;
    let logMessages;

    beforeEach(() => {
        logMessages = [];
        global.console = { log: (msg) => logMessages.push(msg) };
        global.Xrm = createMockXrm({ userRoles: [] });

        formContext = createMockFormContext(
            {
                name: 'Old Account Name',
                telephone1: '6133254463',
                telephone2: null,
                fax: '6131112222',
                emailaddress1: 'test@example.com',
                new_accounttypeid: null,
                new_accountsubtypeid: null,
                new_accounttypelocked: false,
                new_accountsubtypelocked: false,
            },
            {
                controls: ['new_accounttypeid', 'new_accountsubtypeid'],
                tabs: {
                    SUMMARY_TAB: ['SUMMARY_TAB_section_5'],
                },
                navItems: [
                    'nav_new_events_accountvenues',
                    'nav_new_account_event_Committee',
                ],
            }
        );

        form = new CMAConnectForm(formContext);
        form.initialize();
    });

    // ─── Initialization ──────────────────────────────────────────────────────────

    it('should initialize the CMAConnect form', () => {
        expect(form).to.be.instanceOf(CMAConnectForm);
    });

    it('is an instance of AccountBaseForm', () => {
        expect(form).to.be.instanceOf(AccountBaseForm);
    });

    it('logs the CMA Connect Form initialization message', () => {
        const msg = logMessages.find((m) => m.includes('CMA Connect Form initializing'));
        expect(msg).to.not.be.undefined;
    });

    // ─── Phone formatting ────────────────────────────────────────────────────────

    it('formats telephone1 on load', () => {
        expect(formContext.getAttribute('telephone1').getValue()).to.equal('(613) 325-4463');
    });

    it('formats telephone1 when the field value changes', () => {
        formContext.getAttribute('telephone1').setValue('6133254463');
        formContext.getAttribute('telephone1').fireOnChange();
        expect(formContext.getAttribute('telephone1').getValue()).to.equal('(613) 325-4463');
    });

    // ─── OnChange_ShowHideCommitteeEventInformation ──────────────────────────────

    describe('OnChange_ShowHideCommitteeEventInformation()', () => {
        function buildCtx(accountTypeName) {
            const fc = createMockFormContext(
                {
                    new_accounttypeid: accountTypeName
                        ? [{ id: 'type-guid', name: accountTypeName }]
                        : null,
                },
                {
                    tabs: { SUMMARY_TAB: ['SUMMARY_TAB_section_5'] },
                    navItems: [
                        'nav_new_events_accountvenues',
                        'nav_new_account_event_Committee',
                    ],
                }
            );
            return { getFormContext: () => fc, fc };
        }

        it('shows the committee section for a "Committee" account type', () => {
            const { getFormContext, fc } = buildCtx('Committee – National');
            form.OnChange_ShowHideCommitteeEventInformation({ getFormContext });
            const section = fc.ui.tabs.get('SUMMARY_TAB').sections.get('SUMMARY_TAB_section_5');
            expect(section.getVisible()).to.be.true;
        });

        it('hides the committee section for a non-committee account type', () => {
            const { getFormContext, fc } = buildCtx('Corporate');
            form.OnChange_ShowHideCommitteeEventInformation({ getFormContext });
            const section = fc.ui.tabs.get('SUMMARY_TAB').sections.get('SUMMARY_TAB_section_5');
            expect(section.getVisible()).to.be.false;
        });

        it('shows the event nav item for an "Event Location" account type', () => {
            const { getFormContext, fc } = buildCtx('Event Location – Downtown');
            form.OnChange_ShowHideCommitteeEventInformation({ getFormContext });
            const nav = fc.ui.navigation.items.get('nav_new_events_accountvenues');
            expect(nav.getVisible()).to.be.true;
        });

        it('shows the committee nav item for a "Committee" account type', () => {
            const { getFormContext, fc } = buildCtx('Committee – Regional');
            form.OnChange_ShowHideCommitteeEventInformation({ getFormContext });
            const nav = fc.ui.navigation.items.get('nav_new_account_event_Committee');
            expect(nav.getVisible()).to.be.true;
        });

        it('hides both nav items when type is neither Committee nor Event Location', () => {
            const { getFormContext, fc } = buildCtx('Corporate');
            form.OnChange_ShowHideCommitteeEventInformation({ getFormContext });
            expect(fc.ui.navigation.items.get('nav_new_events_accountvenues').getVisible()).to.be.false;
            expect(fc.ui.navigation.items.get('nav_new_account_event_Committee').getVisible()).to.be.false;
        });
    });

    // ─── OnChange_ShowClearAccountSubType ─────────────────────────────────────────

    describe('OnChange_ShowClearAccountSubType()', () => {
        it('clears the subtype field when called without isFormLoad', () => {
            global.Xrm = createMockXrm({ webApiEntities: [] });
            formContext.getAttribute('new_accountsubtypeid').setValue('some-subtype');

            const ctx = { getFormContext: () => formContext };
            form.OnChange_ShowClearAccountSubType(ctx, false);

            expect(formContext.getAttribute('new_accountsubtypeid').getValue()).to.be.null;
        });

        it('preserves the subtype field when isFormLoad is true', () => {
            global.Xrm = createMockXrm({ webApiEntities: [] });
            formContext.getAttribute('new_accountsubtypeid').setValue('keep-me');
            formContext.getAttribute('new_accounttypeid').setValue([{ id: 'type-001', name: 'Corporate' }]);

            const ctx = { getFormContext: () => formContext };
            form.OnChange_ShowClearAccountSubType(ctx, true);

            expect(formContext.getAttribute('new_accountsubtypeid').getValue()).to.equal('keep-me');
        });
    });

    // ─── FilterAccountTypesByAccessField ─────────────────────────────────────────

    describe('FilterAccountTypesByAccessField()', () => {
        // Use a fresh form without initialize() to avoid pre-existing filters from OnLoad_SetTypeFilters
        function freshForm() {
            const fc = createMockFormContext(
                { new_accounttypeid: null },
                { controls: ['new_accounttypeid'] }
            );
            return { fc, form: new CMAConnectForm(fc) };
        }

        it('adds a preSearch handler to the specified control', () => {
            const { fc, form: f } = freshForm();
            f.FilterAccountTypesByAccessField('new_accounttypeid');
            const ctrl = fc.getControl('new_accounttypeid');
            ctrl.triggerPreSearch();
            expect(ctrl.getCustomFilters()).to.have.length(1);
        });

        it('filter XML restricts on new_accesscontrollocked', () => {
            const { fc, form: f } = freshForm();
            f.FilterAccountTypesByAccessField('new_accounttypeid');
            const ctrl = fc.getControl('new_accounttypeid');
            ctrl.triggerPreSearch();
            expect(ctrl.getCustomFilters()[0]).to.include('new_accesscontrollocked');
        });

        it('does nothing when the control does not exist', () => {
            expect(() => form.FilterAccountTypesByAccessField('nonexistent_field')).to.not.throw();
        });
    });

    // ─── OnLoad_SetTypeFilters ────────────────────────────────────────────────────

    describe('OnLoad_SetTypeFilters()', () => {
        it('applies access field filters when user has no eligible roles', () => {
            global.Xrm = createMockXrm({ userRoles: [] }); // no roles
            form.OnLoad_SetTypeFilters();

            const ctrl = formContext.getControl('new_accounttypeid');
            ctrl.triggerPreSearch();
            expect(ctrl.getCustomFilters().length).to.be.greaterThan(0);
        });

        it('does not apply filters when user has an eligible role', () => {
            global.Xrm = createMockXrm({
                userRoles: ['8406f723-55d7-e111-9e3b-005056a04b45'],
            });
            // Re-create form so initialize() ran with the admin role mock
            const fc = createMockFormContext(
                { new_accounttypeid: null, new_accountsubtypeid: null,
                  new_accounttypelocked: false, new_accountsubtypelocked: false },
                { controls: ['new_accounttypeid', 'new_accountsubtypeid'] }
            );
            const f = new CMAConnectForm(fc);
            f.OnLoad_SetTypeFilters();

            const ctrl = fc.getControl('new_accounttypeid');
            ctrl.triggerPreSearch();
            expect(ctrl.getCustomFilters()).to.have.length(0);
        });
    });
});
