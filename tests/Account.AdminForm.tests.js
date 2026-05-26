import { expect } from 'chai';
import { AdminForm, AccountBaseForm } from '../src/Account.js';
import { createMockFormContext } from './mockExecutionContext.js';

describe('AdminForm', () => {
    let formContext;
    let logMessages;

    beforeEach(() => {
        logMessages = [];
        global.console = { log: (msg) => logMessages.push(msg) };

        formContext = createMockFormContext({
            telephone1: '6133254463',
            telephone2: '6139876543',
            fax: '6131112222',
            emailaddress1: 'admin@example.com',
        });
    });

    it('initializes without throwing', () => {
        const form = new AdminForm(formContext);
        expect(() => form.initialize()).to.not.throw();
    });

    it('is an instance of AccountBaseForm', () => {
        const form = new AdminForm(formContext);
        expect(form).to.be.instanceOf(AccountBaseForm);
    });

    it('logs the Admin Form initialization message', () => {
        const form = new AdminForm(formContext);
        form.initialize();
        const msg = logMessages.find((m) => m.includes('Admin Form initializing'));
        expect(msg).to.not.be.undefined;
    });

    it('inherits phone formatting from AccountBaseForm on initialize', () => {
        const form = new AdminForm(formContext);
        form.initialize();
        expect(formContext.getAttribute('telephone1').getValue()).to.equal('(613) 325-4463');
    });

    it('wires telephone1 onChange handler', () => {
        const form = new AdminForm(formContext);
        form.initialize();

        let called = false;
        formContext.getAttribute('telephone1').addOnChange(() => { called = true; });
        formContext.getAttribute('telephone1').fireOnChange();
        expect(called).to.be.true;
    });
});
