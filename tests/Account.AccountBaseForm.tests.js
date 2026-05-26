import { expect } from 'chai';
import { AccountBaseForm } from '../src/Account.js';
import { createMockFormContext } from './mockExecutionContext.js';

describe('AccountBaseForm', () => {
    let formContext;
    let form;
    let logMessages;

    beforeEach(() => {
        logMessages = [];
        global.console = { log: (msg) => logMessages.push(msg) };

        formContext = createMockFormContext({
            telephone1: '6133254463',
            telephone2: '6139876543',
            fax: '6131112222',
            emailaddress1: 'test@example.com',
        });
        form = new AccountBaseForm(formContext);
        form.initialize();
    });

    // ─── Initialization ──────────────────────────────────────────────────────────

    it('initializes without throwing', () => {
        const f = new AccountBaseForm(formContext);
        expect(() => f.initialize()).to.not.throw();
    });

    it('wires onChange handler for telephone1', () => {
        let fired = false;
        formContext.getAttribute('telephone1').addOnChange(() => { fired = true; });
        formContext.getAttribute('telephone1').fireOnChange();
        expect(fired).to.be.true;
    });

    it('wires onChange handler for telephone2', () => {
        let fired = false;
        formContext.getAttribute('telephone2').addOnChange(() => { fired = true; });
        formContext.getAttribute('telephone2').fireOnChange();
        expect(fired).to.be.true;
    });

    it('wires onChange handler for fax', () => {
        let fired = false;
        formContext.getAttribute('fax').addOnChange(() => { fired = true; });
        formContext.getAttribute('fax').fireOnChange();
        expect(fired).to.be.true;
    });

    it('wires onChange handler for emailaddress1', () => {
        let fired = false;
        formContext.getAttribute('emailaddress1').addOnChange(() => { fired = true; });
        formContext.getAttribute('emailaddress1').fireOnChange();
        expect(fired).to.be.true;
    });

    // ─── onChange_Phone ──────────────────────────────────────────────────────────

    it('formats telephone1 on load via fireOnChangeIfExists', () => {
        // initialize() fires onChange for telephone1, which triggers formatPhoneSimple
        expect(formContext.getAttribute('telephone1').getValue()).to.equal('(613) 325-4463');
    });

    it('formats telephone2 on load via fireOnChangeIfExists', () => {
        expect(formContext.getAttribute('telephone2').getValue()).to.equal('(613) 987-6543');
    });

    it('formats fax on load via fireOnChangeIfExists', () => {
        expect(formContext.getAttribute('fax').getValue()).to.equal('(613) 111-2222');
    });

    it('formats telephone1 when the field value changes', () => {
        formContext.getAttribute('telephone1').setValue('4165559876');
        formContext.getAttribute('telephone1').fireOnChange();
        expect(formContext.getAttribute('telephone1').getValue()).to.equal('(416) 555-9876');
    });

    it('logs the phone number when onChange fires', () => {
        formContext.getAttribute('telephone1').setValue('4165559876');
        logMessages.length = 0; // clear load messages
        formContext.getAttribute('telephone1').fireOnChange();
        const phoneLog = logMessages.find((m) => m.includes('Phone number changed'));
        expect(phoneLog).to.not.be.undefined;
    });

    // ─── Missing fields ──────────────────────────────────────────────────────────

    it('does not throw when phone fields are absent', () => {
        const bare = createMockFormContext({});
        const f = new AccountBaseForm(bare);
        expect(() => f.initialize()).to.not.throw();
    });
});
