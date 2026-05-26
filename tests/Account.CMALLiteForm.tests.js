import { expect } from 'chai';
import { CMALLiteForm, AccountBaseForm } from '../src/Account.js';
import { createMockFormContext } from './mockExecutionContext.js';

describe('CMALLiteForm', () => {
    let logMessages;

    beforeEach(() => {
        logMessages = [];
        global.console = { log: (msg) => logMessages.push(msg) };
    });

    function buildFormContext(formType = 2) {
        return createMockFormContext(
            {
                telephone1: '6133254463',
                primarycontactid: null,
            },
            {
                formType,
                entityId: 'acct-001',
                controls: ['primarycontactid'],
            }
        );
    }

    it('initializes without throwing', () => {
        const formContext = buildFormContext();
        const form = new CMALLiteForm(formContext);
        expect(() => form.initialize()).to.not.throw();
    });

    it('is an instance of AccountBaseForm', () => {
        const form = new CMALLiteForm(buildFormContext());
        expect(form).to.be.instanceOf(AccountBaseForm);
    });

    it('logs the CMA Lite Form initialization message', () => {
        const formContext = buildFormContext();
        const form = new CMALLiteForm(formContext);
        form.initialize();
        const msg = logMessages.find((m) => m.includes('CMA Lite Form initializing'));
        expect(msg).to.not.be.undefined;
    });

    // ─── OnLoad_SetPrimaryContactFilter ──────────────────────────────────────────

    describe('OnLoad_SetPrimaryContactFilter()', () => {
        it('exits early on a create form (formType = 1) without adding a filter', () => {
            const formContext = buildFormContext(1); // create form
            const form = new CMALLiteForm(formContext);
            form.initialize();

            const ctrl = formContext.getControl('primarycontactid');
            ctrl.triggerPreSearch();
            // No custom filter should have been added
            expect(ctrl.getCustomFilters()).to.have.length(0);
        });

        it('adds a preSearch filter on an existing record (formType = 2)', () => {
            const formContext = buildFormContext(2); // update form
            const form = new CMALLiteForm(formContext);
            form.initialize();

            const ctrl = formContext.getControl('primarycontactid');
            ctrl.triggerPreSearch();
            expect(ctrl.getCustomFilters()).to.have.length(1);
        });

        it('includes the account ID in the filter XML', () => {
            const formContext = buildFormContext(2);
            const form = new CMALLiteForm(formContext);
            form.initialize();

            const ctrl = formContext.getControl('primarycontactid');
            ctrl.triggerPreSearch();
            expect(ctrl.getCustomFilters()[0]).to.include('acct-001');
        });

        it('filters on connection record1id', () => {
            const formContext = buildFormContext(2);
            const form = new CMALLiteForm(formContext);
            form.initialize();

            const ctrl = formContext.getControl('primarycontactid');
            ctrl.triggerPreSearch();
            expect(ctrl.getCustomFilters()[0]).to.include('record1id');
        });

        it('does nothing when primarycontactid control is absent', () => {
            const formContext = createMockFormContext({ telephone1: '6133254463' }, { formType: 2 });
            const form = new CMALLiteForm(formContext);
            expect(() => form.initialize()).to.not.throw();
        });
    });
});
