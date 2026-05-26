import { expect } from 'chai';
import { BaseForm } from '../src/BaseForm.js';
import { createMockFormContext, createMockExecutionContext, createMockXrm } from './mockExecutionContext.js';

describe('BaseForm', () => {
    let formContext;
    let form;
    let logMessages;

    beforeEach(() => {
        logMessages = [];
        global.console = { log: (msg) => logMessages.push(msg) };
        global.Xrm = createMockXrm();

        formContext = createMockFormContext(
            { telephone1: '6133254463', emailaddress1: 'test@example.com', age: '25abc' },
        );
        form = new BaseForm(formContext);
    });

    // ─── Logging ────────────────────────────────────────────────────────────────

    describe('log()', () => {
        it('outputs an INFO-prefixed message', () => {
            form.log('hello');
            expect(logMessages[0]).to.include('INFO');
            expect(logMessages[0]).to.include('hello');
        });

        it('includes the entity name', () => {
            form.log('hello');
            expect(logMessages[0]).to.include('account');
        });
    });

    describe('warn()', () => {
        it('outputs a WARN-prefixed message', () => {
            form.warn('something off');
            expect(logMessages[0]).to.include('WARN');
            expect(logMessages[0]).to.include('something off');
        });
    });

    describe('error()', () => {
        it('outputs an ERROR-prefixed message with exception', () => {
            form.error('bad thing', new Error('boom'));
            expect(logMessages[0]).to.include('ERROR');
            expect(logMessages[0]).to.include('bad thing');
            expect(logMessages[0]).to.include('boom');
        });
    });

    // ─── getAttr ────────────────────────────────────────────────────────────────

    describe('getAttr()', () => {
        it('returns the attribute when it exists', () => {
            const attr = form.getAttr('telephone1');
            expect(attr).to.not.be.null;
            expect(attr.getValue()).to.equal('6133254463');
        });

        it('returns null when the attribute does not exist', () => {
            expect(form.getAttr('nonexistent')).to.be.null;
        });
    });

    // ─── onChange_EmailAddress ───────────────────────────────────────────────────

    describe('onChange_EmailAddress()', () => {
        it('returns true for a valid email', () => {
            expect(form.onChange_EmailAddress('user@example.com')).to.be.true;
        });

        it('returns true for email with subdomains', () => {
            expect(form.onChange_EmailAddress('user@mail.example.co.uk')).to.be.true;
        });

        it('returns false when @ is missing', () => {
            expect(form.onChange_EmailAddress('userexample.com')).to.be.false;
        });

        it('returns false when domain is missing', () => {
            expect(form.onChange_EmailAddress('user@')).to.be.false;
        });

        it('returns false for empty string', () => {
            expect(form.onChange_EmailAddress('')).to.be.false;
        });
    });

    // ─── setupOnChangeIfFieldExists ──────────────────────────────────────────────

    describe('setupOnChangeIfFieldExists()', () => {
        it('wires the handler and calls it when the field changes', () => {
            let called = false;
            form.setupOnChangeIfFieldExists('telephone1', () => { called = true; });
            formContext.getAttribute('telephone1').fireOnChange();
            expect(called).to.be.true;
        });

        it('does nothing when the field is not on the form', () => {
            // Should not throw
            expect(() => form.setupOnChangeIfFieldExists('missing_field', () => {})).to.not.throw();
        });
    });

    // ─── fireOnChangeIfExists ────────────────────────────────────────────────────

    describe('fireOnChangeIfExists()', () => {
        it('fires onChange handlers for an existing field', () => {
            let fired = false;
            formContext.getAttribute('telephone1').addOnChange(() => { fired = true; });
            form.fireOnChangeIfExists('telephone1');
            expect(fired).to.be.true;
        });

        it('does nothing when the field does not exist', () => {
            expect(() => form.fireOnChangeIfExists('missing_field')).to.not.throw();
        });
    });

    // ─── formatPhoneSimple ───────────────────────────────────────────────────────

    describe('formatPhoneSimple()', () => {
        it('formats a 10-digit number into (NXX) NXX-XXXX', () => {
            form.formatPhoneSimple('telephone1'); // value is '6133254463'
            expect(formContext.getAttribute('telephone1').getValue()).to.equal('(613) 325-4463');
        });

        it('formats an 11-digit number starting with 1', () => {
            formContext.getAttribute('telephone1').setValue('16133254463');
            form.formatPhoneSimple('telephone1');
            expect(formContext.getAttribute('telephone1').getValue()).to.equal('(613) 325-4463');
        });

        it('leaves an already-short number unchanged', () => {
            formContext.getAttribute('telephone1').setValue('12345');
            form.formatPhoneSimple('telephone1');
            expect(formContext.getAttribute('telephone1').getValue()).to.equal('12345');
        });

        it('does nothing when the field does not exist', () => {
            expect(() => form.formatPhoneSimple('nonexistent')).to.not.throw();
        });
    });

    // ─── validateNumberInput ─────────────────────────────────────────────────────

    describe('validateNumberInput()', () => {
        it('clears the field when the value is non-numeric', () => {
            const ctx = createMockExecutionContext('account', { age: '25abc' });
            form.validateNumberInput(ctx, 'age');
            expect(ctx.getFormContext().getAttribute('age').getValue()).to.be.null;
        });

        it('preserves the field value when it is purely numeric', () => {
            const ctx = createMockExecutionContext('account', { age: '42' });
            form.validateNumberInput(ctx, 'age');
            expect(ctx.getFormContext().getAttribute('age').getValue()).to.equal('42');
        });

        it('does nothing when the field value is null', () => {
            const ctx = createMockExecutionContext('account', { age: null });
            expect(() => form.validateNumberInput(ctx, 'age')).to.not.throw();
        });
    });
});
