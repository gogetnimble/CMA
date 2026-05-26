import { expect } from 'chai';
import { ContactBaseForm, ContactMainForm } from '../src/Contact.js';
import { createMockFormContext } from './mockExecutionContext.js';

describe('Contact Forms', () => {
    let formContext;
    let logMessages;

    beforeEach(() => {
        logMessages = [];
        global.console = { log: (msg) => logMessages.push(msg) };

        formContext = createMockFormContext({ firstname: 'Jane' });
    });

    // ─── ContactBaseForm ─────────────────────────────────────────────────────────

    describe('ContactBaseForm', () => {
        it('initializes without throwing', () => {
            const form = new ContactBaseForm(formContext);
            expect(() => form.initialize()).to.not.throw();
        });

        it('wires an onChange handler to the firstname field', () => {
            const form = new ContactBaseForm(formContext);
            form.initialize();

            let called = false;
            formContext.getAttribute('firstname').addOnChange(() => { called = true; });
            formContext.getAttribute('firstname').fireOnChange();
            expect(called).to.be.true;
        });

        it('logs the new first name when the firstname field changes', () => {
            const form = new ContactBaseForm(formContext);
            form.initialize();

            formContext.getAttribute('firstname').setValue('John');
            formContext.getAttribute('firstname').fireOnChange();

            const loggedNames = logMessages.filter((m) => m.includes('John'));
            expect(loggedNames).to.have.length.greaterThan(0);
        });

        it('does not throw when firstname field is absent', () => {
            const ctxNoName = createMockFormContext({});
            const form = new ContactBaseForm(ctxNoName);
            expect(() => form.initialize()).to.not.throw();
        });
    });

    // ─── ContactMainForm ─────────────────────────────────────────────────────────

    describe('ContactMainForm', () => {
        it('initializes without throwing', () => {
            const form = new ContactMainForm(formContext);
            expect(() => form.initialize()).to.not.throw();
        });

        it('is an instance of ContactBaseForm', () => {
            const form = new ContactMainForm(formContext);
            expect(form).to.be.instanceOf(ContactBaseForm);
        });

        it('still wires the firstname onChange from super.initialize()', () => {
            const form = new ContactMainForm(formContext);
            form.initialize();

            let called = false;
            formContext.getAttribute('firstname').addOnChange(() => { called = true; });
            formContext.getAttribute('firstname').fireOnChange();
            expect(called).to.be.true;
        });

        it('logs the main-form extra setup message', () => {
            const form = new ContactMainForm(formContext);
            form.initialize();

            const mainFormLog = logMessages.find((m) => m.includes('Main Form extra setup'));
            expect(mainFormLog).to.not.be.undefined;
        });
    });
});
